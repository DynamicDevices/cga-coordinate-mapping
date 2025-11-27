using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

public class UWB2GPSConverter
{
    [System.Serializable]
    public class UWB
    {
        public string id;
        public int triageStatus;
        [JsonIgnore]
        public Vector3 position;
        public double[] latLonAlt;
        public bool positionKnown;
        public float lastPositionUpdateTime;
        [JsonIgnore] //Never needs to be sent externally
        public bool positionFoundThisPass;
        public List<Edge> edges;
        public float positionAccuracy; // in meters, optional accuracy estimate for the position
        public string positionSource;
        public float positionConfidence;
        public float battery;
        public float temperature;
        public float humidity;
        public float rssi;
        public float snr;
        public float loraGatewayCount;
        public float loraFrameCount;
        public float loraPort;
        public string loraDeviceId;
        public float loraDataTimestamp;
        public string loraReceivedAt;
        public float lora_fix_type;
        public float lora_satellites;

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
    {
        public UWB[] uwbs;
        public Network() { }
        public Network(UWB[] uwbs) => this.uwbs = uwbs;
    }





    public static void ConvertUWBToPositions(Network network, bool refine)
    {
        float timeNow = (float)DateTime.UtcNow.TimeOfDay.TotalSeconds;
        UWB[] allNodes = network.uwbs;

        // First pass - get initial positions using trilateration
        // 1. Find all unique nodes in the network
        int totalNodes = allNodes.Length;
        int totalNodesUpdated = 0;

        // 2. Find the 3 nodes with positionKnown == true
        List<UWB> knownNodes = new List<UWB>();
        foreach (UWB node in allNodes)
        {
            if (node.positionKnown && node.latLonAlt != null && node.latLonAlt.Length == 3 && node.latLonAlt[0] != 0 && node.latLonAlt[1] != 0)
            {
                knownNodes.Add(node);
                if (knownNodes.Count == 3) break;
            }
            node.positionFoundThisPass = false;
        }

        if (knownNodes.Count < 3)
        {
            Console.Error.WriteLine("Not enough known nodes for triangulation. You need 3 beacons with positionKnown = true and lat/lon/alts set");
            return;
        }

        //3. Get the positions of the known Nodes (beacons)
        //Get latLonAlt ref point from the first known node
        double refPointLat = knownNodes[0].latLonAlt[0];
        double refPointLon = knownNodes[0].latLonAlt[1];
        double refPointAlt = knownNodes[0].latLonAlt[2] / 1000d;
        Vector3 refPos = knownNodes[0].position;
        knownNodes[0].positionFoundThisPass = true;
        //Use that ref point to get all relative position of any nodes that have latLonAlts
        foreach (UWB node in allNodes)
        {
            if (node != knownNodes[0] && node.latLonAlt != null && node.latLonAlt.Length == 3 && node.latLonAlt[0] != 0 && node.latLonAlt[1] != 0)
            {
                node.position =
                WGS84Converter.LatLonAltkm2UnityPos(refPointLat, refPointLon, refPointAlt,
                node.latLonAlt[0], node.latLonAlt[1], node.latLonAlt[2] / 1000d,
                refPos);
                node.positionFoundThisPass = true;
            }
        }

        // 4. Iteratively update positions for unknown nodes
        bool progress = true;
        while (progress)
        {
            progress = false;
            foreach (UWB node in allNodes)
            {
                if (node.positionFoundThisPass) continue;

                // If the node is not known, try to update its position
                UWB[] triangulationNodes = new UWB[3];
                float[] distances = new float[3];

                int index = 0;
                foreach (Edge edge in node.edges)
                {
                    if (TryGetEndFromEdge(edge, network, out UWB end))
                    {
                        if (end.positionFoundThisPass)
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
                    //Debug.Log($"Not enough triangulation nodes yet for node {node.id} - skip over this time");
                    continue;
                }

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
                    Console.Error.WriteLine($"Cannot triangulate node {node.id} with triangulation nodes {triangulationNodes[0].id}, {triangulationNodes[1].id} and {triangulationNodes[2].id} : the nodes are collinear or too close.");
                    continue;
                }

                float x = (r0 * r0 - r1 * r1 + d * d) / (2 * d);
                float y = (r0 * r0 - r2 * r2 + i * i + j * j - 2 * i * x) / (2 * j);

                float zSquared = r0 * r0 - x * x - y * y;
                float z = zSquared > 0 ? (float)Math.Sqrt(zSquared) : 0;

                node.position = p0 + x * ex + y * ey + z * ez;
                node.positionFoundThisPass = true;
                totalNodesUpdated++;
                progress = true;
            }
        }

        // Second pass - iterative refinement
        if (refine)
        {
            const int MAX_ITERATIONS = 10;
            const float LEARNING_RATE = 0.1f;
            for (int iter = 0; iter < MAX_ITERATIONS; iter++)
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
                        UWB neighbour = null;
                        foreach (UWB other in allNodes)
                        {
                            if (other.id == edge.end1)
                            {
                                neighbour = other;
                            }
                        }

                        if (neighbour == null || !neighbour.positionFoundThisPass) continue;

                        float currentDist = Vector3.Distance(node.position, neighbour.position);
                        float error = currentDist - edge.distance;

                        Vector3 direction = (node.position - neighbour.position).Normalized();
                        gradient += direction * error;
                    }

                    // Apply gradient descent update
                    Vector3 newPos = node.position - gradient * LEARNING_RATE;

                    // Check if new position reduces total error
                    float oldError = NodeError(node, network);
                    node.position = newPos;
                    float newError = NodeError(node, network);

                    if (newError < oldError)
                    {
                        improved = true;
                        //Debug.Log($"Position improved for node {node.id} to {node.position}\n");
                    }
                    else
                    {
                        node.position = originalPos;
                    }
                }

                if (!improved) break;
            }
        }



        float totalError = 0;
        float total = 0;
        float numEstimated = 0;
        foreach (UWB node in network.uwbs)
        {
            if (!node.positionKnown)
            {
                node.latLonAlt = WGS84Converter.LatLonAltEstimate(refPointLat, refPointLon, refPointAlt, refPos, node.position);
                if (node.positionFoundThisPass)
                {
                    UWBManager.AddToDebugMessage($"Position triangulated for node {node.id} to {node.latLonAlt[0]}, {node.latLonAlt[1]}, {node.latLonAlt[2]}");
                    node.lastPositionUpdateTime = timeNow;
                }
                else
                {
                    UWBManager.AddToDebugMessage($"Position estimated for node {node.id} to {node.latLonAlt[0]}, {node.latLonAlt[1]}, {node.latLonAlt[2]}");
                    numEstimated++;
                }
            }
            totalError += NodeError(node, network);
            total++;
        }
        total *= 0.5f;

        string m = $"UWB to GPS conversion completed. Triangulated {totalNodesUpdated}/{totalNodes} positions. Estimated {numEstimated}/{totalNodes} positions. Average error: {totalError / total}m.";
        List<UWB> untriangulated = new List<UWB>();
        List<UWB> badTags = new List<UWB>();
        List<UWB> badAnchorsEdges = new List<UWB>();
        List<UWB> badAnchorsLatLons = new List<UWB>();
        foreach (UWB node in allNodes)
        {
            if (!node.positionFoundThisPass)
            {
                untriangulated.Add(node);
            }
            int numEdges = node.edges.Count();
            if (node.positionKnown)
            {
                if (numEdges < 3)
                {
                    badAnchorsEdges.Add(node);
                }
                else if (node.latLonAlt == null || node.latLonAlt.Length != 3 || node.latLonAlt[0] == 0 || node.latLonAlt[1] == 0)
                {
                    badAnchorsLatLons.Add(node);
                }
            }
            else
            {
                if (numEdges < 3)
                {
                    badTags.Add(node);
                }
            }
        }

        int numUntriangulated = untriangulated.Count;
        if (numUntriangulated == 0)
        {
            m += $"\n All nodes triangulated.";
        }
        else
        {
            m += $"\n {numUntriangulated} nodes not triangulated: ";
            foreach (UWB node in untriangulated)
            {
                m += $"{node.id}, ";
            }

            m += $"\n Triangulation Failure Reasons: ";
            bool foundReason = false;
            if (badTags.Count > 0)
            {
                m += $"\n Tags found with less than 3 edges: ";
                foreach (UWB node in badTags)
                {
                    m += node.id + ", ";
                }
                foundReason = true;
            }
            if (badAnchorsEdges.Count > 0)
            {
                m += $"\n Anchors found with less than 3 edges: ";
                foreach (UWB node in badAnchorsEdges)
                {
                    m += node.id + ", ";
                }
                foundReason = true;
            }
            if (badAnchorsLatLons.Count > 0)
            {
                m += $"\n Anchors found with no latLonAlts: ";
                foreach (UWB node in badAnchorsLatLons)
                {
                    m += node.id + ", ";
                }
                foundReason = true;
            }
            if(!foundReason)
            {
                m += $"\n No obvious reason found - Network is probably in more than one cluster - disjointed groups. If you get this message and want further information, much deeper debug code will need to be written to verify.";
            }
        }

        UWBManager.AddToDebugMessage(m);        

    }

    private static float NodeError(UWB node, Network network)
    {
        float totalError = 0;
        foreach (Edge edge in node.edges)
        {
            if (TryGetEndFromEdge(edge, network, out UWB end))
            {
                totalError += EdgeErrorSquared(node, end, edge.distance);
            }
        }
        int numEdges = node.edges.Count;
        node.positionAccuracy = numEdges == 0 ? -1f : (float)Math.Sqrt(totalError / node.edges.Count);
        return totalError;
    }

    private static float EdgeErrorSquared(UWB end0, UWB end1, float edgeDistance)
    {
        float currentDist = Vector3Extensions.Distance(end0.position, end1.position);
        float error = Math.Abs(currentDist - edgeDistance);
        return error * error; // Squared error
    }

    public static bool TryGetEndFromEdge(Edge edge, Network network, out UWB end)
    {
        end = null;
        if (network == null || network.uwbs == null) return false;

        foreach (UWB node in network.uwbs)
        {
            if (node.id == edge.end1)
            {
                end = node;
                return true;
            }
        }
        return false;
    }
}
