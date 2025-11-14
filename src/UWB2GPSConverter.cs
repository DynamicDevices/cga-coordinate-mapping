#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using InstDotNet;

public class UWB2GPSConverter
{
    private static ILogger? _logger;
    private static Dictionary<string, BeaconConfig>? _configuredBeacons;

    [System.Serializable]
#nullable disable  // JSON deserialization classes
    public class UWB
    {
        public string id;
        public TriageStatus triageStatus;
        public enum TriageStatus { unknown, responder, unconscious, injured, deceased, beacon }
        [JsonIgnore]
        public Vector3 position;
        public double[] latLonAlt;
        public bool positionKnown;
        public float lastPositionUpdateTime;
        public List<Edge> edges;
        public float positionAccuracy;

        public UWB() { edges = new List<Edge>(); }  // ensures not null if JSON omits it
        public UWB(string id)
        {
            this.id = id;
            edges = new List<Edge>();
        }
    }

    [System.Serializable]
    public class Edge
    {
        public string end0;
        public string end1;
        public float distance;

        // For System.Text.Json
        public Edge() { }

        // Convenience ctor you already had
        public Edge(UWB end0, UWB end1, float distance)
        {
            this.end0 = end0.id;
            this.end1 = end1.id;
            this.distance = distance;
        }
    }
    [System.Serializable]
    public class Network
#nullable enable
    {
        public UWB[] uwbs = Array.Empty<UWB>();
        public Network() { }
        public Network(UWB[] uwbs) => this.uwbs = uwbs;
    }





    /// <summary>
    /// Initialize beacons from configuration. These beacons will be applied to incoming network data.
    /// </summary>
    /// <param name="beacons">List of beacon configurations with GPS coordinates</param>
    public static void InitializeBeacons(List<BeaconConfig> beacons)
    {
        _configuredBeacons = new Dictionary<string, BeaconConfig>();
        foreach (var beacon in beacons)
        {
            if (!string.IsNullOrEmpty(beacon.Id))
            {
                _configuredBeacons[beacon.Id] = beacon;
            }
        }
    }

    /// <summary>
    /// Apply configured beacon positions to network nodes that match beacon IDs.
    /// Also converts GPS coordinates to 3D positions for all nodes with positionKnown = true.
    /// </summary>
    private static void ApplyConfiguredBeacons(Network network)
    {
        if (network.uwbs == null)
        {
            return;
        }

        // First, apply configured beacons if available
        if (_configuredBeacons != null)
        {
            foreach (var node in network.uwbs)
            {
                if (node == null || string.IsNullOrEmpty(node.id))
                {
                    continue;
                }

                if (_configuredBeacons.TryGetValue(node.id, out var beacon))
                {
                    // Set as known position from configuration
                    node.positionKnown = true;
                    node.latLonAlt = new double[] { beacon.Latitude, beacon.Longitude, beacon.Altitude };
                }
            }
        }

        // Find the first node with positionKnown = true and valid latLonAlt to use as reference
        UWB? referenceNode = null;
        foreach (var node in network.uwbs)
        {
            if (node != null && node.positionKnown && 
                node.latLonAlt != null && node.latLonAlt.Length >= 3)
            {
                referenceNode = node;
                break;
            }
        }

        if (referenceNode == null)
        {
            _logger?.LogWarning("No reference node found with positionKnown = true and valid latLonAlt. Cannot convert GPS to 3D positions.");
            return;
        }

        double refLat = referenceNode.latLonAlt[0];
        double refLon = referenceNode.latLonAlt[1];
        double refAlt = referenceNode.latLonAlt[2];
        Vector3 refPoint = new Vector3(0, 0, 0); // Reference point in Unity space

        // Convert GPS to local 3D position for all nodes with positionKnown = true
        foreach (var node in network.uwbs)
        {
            if (node == null || !node.positionKnown)
            {
                continue;
            }

            if (node.latLonAlt == null || node.latLonAlt.Length < 3)
            {
                _logger?.LogWarning("Node {NodeId} has positionKnown = true but invalid latLonAlt", node.id);
                continue;
            }

            // Convert GPS to local 3D position using reference node
            node.position = WGS84Converter.LatLonAltkm2UnityPos(
                refLat, refLon, refAlt / 1000.0,
                node.latLonAlt[0], node.latLonAlt[1], node.latLonAlt[2] / 1000.0,
                refPoint);
        }
    }

    public static void ConvertUWBToPositions(Network network, bool refine, AlgorithmConfig? algorithmConfig = null)
    {
        // Ensure logger is available - reinitialize if disposed
        if (_logger == null)
        {
            try
            {
                _logger = AppLogger.GetLogger<UWB2GPSConverter>();
            }
            catch (ObjectDisposedException)
            {
                // Logger was disposed, reinitialize it
                AppLogger.Initialize(LogLevel.Information);
                _logger = AppLogger.GetLogger<UWB2GPSConverter>();
            }
        }

        if (network == null || network.uwbs == null || network.uwbs.Length == 0)
        {
            _logger.LogError("ConvertUWBToPositions: network is null or empty.");
            return;
        }

        // Apply configured beacons to network nodes
        ApplyConfiguredBeacons(network);

        float timeNow = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
        UWB[] allNodes = network.uwbs;

        // Create dictionary for O(1) node lookups
        Dictionary<string, UWB> nodeMap = new Dictionary<string, UWB>();
        foreach (UWB node in allNodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.id))
            {
                nodeMap[node.id] = node;
            }
        }

        // First pass - get initial positions using trilateration
        // 1. Find all unique nodes in the network
        int totalNodes = allNodes.Length;
        int totalNodesUpdated = 0;

        // 2. Find the 3 nodes with positionKnown == true
        HashSet<UWB> knownNodes = new HashSet<UWB>();
        foreach (UWB node in allNodes)
        {
            if (node.positionKnown)
            {
                knownNodes.Add(node);
                if (knownNodes.Count == 3) break;
            }
            node.lastPositionUpdateTime = 0;
        }

        if (knownNodes.Count < 3)
        {
            _logger?.LogError("Not enough known nodes for triangulation. You need 3 beacons with positionKnown = true and lat/lon/alts set");
            return;
        }

        // 3. Iteratively update positions for unknown nodes
        bool progress = true;
        while (progress)
        {
            progress = false;
            foreach (UWB node in allNodes)
            {
                if (node.positionKnown || node.lastPositionUpdateTime == timeNow) continue;

                // If the node is not known, try to update its position
                UWB[] triangulationNodes = new UWB[3];
                float[] distances = new float[3];

                int index = 0;
                foreach (Edge edge in node.edges)
                {
                    if (TryGetEndFromEdge(edge, node.id, nodeMap, out UWB? end) && end != null)
                    {
                        if (end.positionKnown || end.lastPositionUpdateTime == timeNow)
                        {
                            triangulationNodes[index] = end;
                            distances[index] = edge.distance;
                            index++;
                            if (index >= 3) break;
                        }
                    }
                }

                if (index < 3)
                {
                    // Not enough triangulation nodes yet - skip this node for now
                    continue;
                }
                //Get latLonAlt ref point from the first known node
                if (triangulationNodes[0].latLonAlt == null || triangulationNodes[0].latLonAlt.Length < 3)
                {
                    _logger?.LogWarning("Node {NodeId} has invalid latLonAlt, skipping triangulation for {TargetNodeId}", triangulationNodes[0].id, node.id);
                    continue;
                }
                double refPointLat = triangulationNodes[0].latLonAlt[0];
                double refPointLon = triangulationNodes[0].latLonAlt[1];
                double refPointAlt = triangulationNodes[0].latLonAlt[2];
                Vector3 refPos = triangulationNodes[0].position;

                // Use the first three for triangulation
                Vector3 p0 = triangulationNodes[0].position;
                Vector3 p1 = triangulationNodes[1].position;
                Vector3 p2 = triangulationNodes[2].position;
                float r0 = distances[0];
                float r1 = distances[1];
                float r2 = distances[2];

                // Trilateration in 3D
                Vector3 ex = (p1 - p0).Normalized();
                float i = Vector3Extensions.Dot(ex, p2 - p0);
                Vector3 ey = (p2 - p0 - i * ex).Normalized();
                Vector3 ez = Vector3Extensions.Cross(ex, ey);

                float d = Vector3Extensions.Distance(p0, p1);
                float j = Vector3Extensions.Dot(ey, p2 - p0);

                if (Math.Abs(j) < 1e-6f)
                {
                    _logger?.LogWarning("Cannot triangulate node {NodeId} with triangulation nodes {Node0}, {Node1}, {Node2}: the nodes are collinear or too close.", 
                        node.id, triangulationNodes[0].id, triangulationNodes[1].id, triangulationNodes[2].id);
                    continue;
                }

                float x = (r0 * r0 - r1 * r1 + d * d) / (2 * d);
                float y = (r0 * r0 - r2 * r2 + i * i + j * j - 2 * i * x) / (2 * j);

                float zSquared = r0 * r0 - x * x - y * y;
                float z = zSquared > 0 ? (float)Math.Sqrt(zSquared) : 0;

                node.position = p0 + x * ex + y * ey + z * ez;
                node.latLonAlt = WGS84Converter.LatLonAltEstimate(refPointLat, refPointLon, refPointAlt, refPos, node.position);
                node.lastPositionUpdateTime = timeNow;
                totalNodesUpdated++;
                progress = true;
            }
        }

        // Second pass - iterative refinement
        if (refine)
        {
            int maxIterations = algorithmConfig?.MaxIterations ?? 10;
            float learningRate = algorithmConfig?.LearningRate ?? 0.1f;
            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool improved = false;
                foreach (UWB node in allNodes)
                {
                    if (node.positionKnown) continue;

                    Vector3 originalPos = node.position;
                    Vector3 gradient = Vector3Extensions.Zero;

                    // Calculate gradient based on distance constraints
                    foreach (Edge edge in node.edges)
                    {
                        if (!TryGetEndFromEdge(edge, node.id, nodeMap, out UWB? neighbour) || neighbour == null)
                            continue;

                        if (!neighbour.lastPositionUpdateTime.Equals(timeNow)) continue;

                        float currentDist = Vector3.Distance(node.position, neighbour.position);
                        float error = currentDist - edge.distance;

                        Vector3 direction = (node.position - neighbour.position).Normalized();
                        gradient += direction * error;
                    }

                    // Apply gradient descent update
                    Vector3 newPos = node.position - gradient * learningRate;

                    // Check if new position reduces total error
                    float oldError = NodeError(node, network, nodeMap);
                    node.position = newPos;
                    float newError = NodeError(node, network, nodeMap);

                    if (newError < oldError)
                    {
                        improved = true;
                        node.lastPositionUpdateTime = timeNow;
                    }
                    else
                    {
                        node.position = originalPos;
                    }
                }

                if (!improved) break;
            }
        }

        // Calculate average error across all edges
        float totalError = 0;
        int totalEdges = 0;
        float maxError = 0;
        float minError = float.MaxValue;
        
        foreach (UWB node in network.uwbs)
        {
            foreach (Edge edge in node.edges)
            {
                if (TryGetEndFromEdge(edge, node.id, nodeMap, out UWB? end) && end != null)
                {
                    float currentDist = Vector3Extensions.Distance(node.position, end.position);
                    float error = Math.Abs(currentDist - edge.distance);
                    totalError += error;
                    totalEdges++;
                    if (error > maxError) maxError = error;
                    if (error < minError) minError = error;
                }
            }
        }

        float averageError = totalEdges > 0 ? totalError / totalEdges : 0;

        _logger?.LogInformation("UWB to GPS conversion completed. Updated {Updated}/{Total} positions. Average error: {Error:F2}m (min: {Min:F2}m, max: {Max:F2}m, edges: {Edges}).", 
            totalNodesUpdated, totalNodes, averageError, minError == float.MaxValue ? 0 : minError, maxError, totalEdges);
        if (totalNodesUpdated + 3 < totalNodes)
        {
            var untriangulatedNodes = new List<string>();
            foreach (UWB node in allNodes)
            {
                if (!node.positionKnown && node.lastPositionUpdateTime < timeNow)
                {
                    untriangulatedNodes.Add(node.id);
                }
            }
            if (untriangulatedNodes.Count > 0)
            {
                _logger?.LogWarning("Could not triangulate nodes: {Nodes}", string.Join(", ", untriangulatedNodes));
            }
        }

    }

    private static float NodeError(UWB node, Network network, Dictionary<string, UWB> nodeMap)
    {
        float totalError = 0;
        foreach (Edge edge in node.edges)
        {
            if (TryGetEndFromEdge(edge, node.id, nodeMap, out UWB? end) && end != null)
            {
                totalError += EdgeErrorSquared(node, end, edge.distance);
            }
        }
        int numEdges = node.edges.Count;
        node.positionAccuracy = numEdges == 0 ? -1f : (float)Math.Sqrt(totalError / node.edges.Count);
        return totalError;
    }

    public static float EdgeErrorSquared(UWB end0, UWB end1, float edgeDistance)
    {
        float currentDist = Vector3Extensions.Distance(end0.position, end1.position);
        float error = Math.Abs(currentDist - edgeDistance);
        return error * error; // Squared error
    }

    public static bool TryGetEndFromEdge(Edge edge, string currentNodeId, Dictionary<string, UWB> nodeMap, out UWB? end)
    {
        end = null;
        if (edge == null || string.IsNullOrEmpty(currentNodeId) || nodeMap == null) return false;

        // Find the OTHER end of the edge (not the current node)
        string? otherEndId = null;
        if (edge.end0 == currentNodeId)
        {
            otherEndId = edge.end1;
        }
        else if (edge.end1 == currentNodeId)
        {
            otherEndId = edge.end0;
        }
        else
        {
            // Current node is not in this edge, return false
            return false;
        }

        if (string.IsNullOrEmpty(otherEndId) || !nodeMap.TryGetValue(otherEndId, out end))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Legacy overload for backward compatibility (uses O(n) lookup).
    /// Prefer the dictionary-based overload for better performance.
    /// </summary>
    /// <param name="edge">The edge to examine</param>
    /// <param name="network">The network containing all nodes</param>
    /// <param name="end">Output parameter for one of the edge's end nodes</param>
    /// <returns>True if an end node was found, false otherwise</returns>
    public static bool TryGetEndFromEdge(Edge edge, Network network, out UWB? end)
    {
        end = null;
        if (network == null || network.uwbs == null || edge == null) return false;

        // This is a fallback - we don't know which node is calling, so check both ends
        // This is less efficient but maintains backward compatibility
        foreach (UWB node in network.uwbs)
        {
            if (node.id == edge.end0 || node.id == edge.end1)
            {
                end = node;
                return true;
            }
        }
        return false;
    }
}

