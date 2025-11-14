"""
UwbParser.py - Parser for UWB (Ultra-Wideband) data
"""

import json

# Beacon positions (latitude, longitude, altitude in meters)
# These are reference points with known positions for trilateration
BEACON_1_LAT_LON_ALT = [53.48514639104522, -2.191785053920114, 0]
BEACON_2_LAT_LON_ALT = [53.48545891792991, -2.19232588314793, 0]
BEACON_3_LAT_LON_ALT = [53.485994341662628, -2.192366069038485, 0]

class UwbParser:
    """Parse and process Ultra-Wideband sensor data."""

    def __init__(self):
        """Initialize the UWB parser."""
        pass

    def parse_edges_to_network(self, edge_data_str):
        """
        Parse edge list string and return a UWB2GPSConverter.Network JSON string.
        
        Args:
            edge_data_str: String in format [["B5A4", "B57A", 1.726], ["B5A4", "B98A", 0.854], ...]
            
        Returns:
            JSON string of UWB2GPSConverter.Network with edges populated
        """
        # Parse the input string to get the edge list
        edges = json.loads(edge_data_str)
        
        # Extract unique UWB IDs
        uwb_ids = set()
        for edge in edges:
            uwb_ids.add(edge[0])
            uwb_ids.add(edge[1])
        
        # Create UWB objects with default values
        uwbs = []
        beacon_positions = [
            BEACON_1_LAT_LON_ALT,
            BEACON_2_LAT_LON_ALT,
            BEACON_3_LAT_LON_ALT
        ]
        beaconIDs = ["B5A4", "B57A", "B98A"]
        
        for idx, uwb_id in enumerate(sorted(uwb_ids)):
            # Check if this UWB is a beacon and get its position if so
            is_beacon = uwb_id in beaconIDs
            beacon_idx = beaconIDs.index(uwb_id) if is_beacon else -1
            
            uwb = {
                "id": uwb_id,
                "triageStatus": 0,  # unknown
                "position": {"x": 0, "y": 0, "z": 0},
                "latLonAlt": beacon_positions[beacon_idx] if is_beacon else [0.0, 0.0, 0.0],
                "positionKnown": is_beacon,
                "lastPositionUpdateTime": 0.0,
                "edges": [],
                "positionAccuracy": -1.0
            }
            uwbs.append(uwb)
        
        # Create a map for quick lookup
        uwb_map = {uwb["id"]: uwb for uwb in uwbs}
        
        # Populate edges
        for edge in edges:
            end0_id = edge[0]
            end1_id = edge[1]
            distance = edge[2]
            
            edge_obj = {
                "end0": end0_id,
                "end1": end1_id,
                "distance": float(distance)
            }
            
            # Add edge to both UWBs
            if end0_id in uwb_map:
                uwb_map[end0_id]["edges"].append(edge_obj)
            if end1_id in uwb_map:
                uwb_map[end1_id]["edges"].append(edge_obj)
        


        # Create Network object
        network = {
            "uwbs": uwbs
        }
        
        return json.dumps(network)


if __name__ == "__main__":
    parser = UwbParser()
    
    # Read edge data from test_uwbs.json
with open("test_uwbs.json", "r") as f:
    edge_data_str = f.read()    
    result = parser.parse_edges_to_network(edge_data_str)
    print("Generated Network JSON:")

    with open("uwb_network.json", "w") as f:
        f.write(result)

