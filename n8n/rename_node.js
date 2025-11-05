const fs = require('fs');
const path = require('path');

const workflowPath = path.join(__dirname, 'workflow', 'SoccerLST.json');
const oldName = 'Normalize previous output';
const newName = 'filter_and_format_previous_match';

// Read workflow
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Find and update the node name
const nodeIndex = workflow.nodes.findIndex(node => node.name === oldName);
if (nodeIndex !== -1) {
    workflow.nodes[nodeIndex].name = newName;
    console.log(`Renamed node from "${oldName}" to "${newName}"`);
} else {
    console.log(`Node "${oldName}" not found`);
    process.exit(1);
}

// Update all connections that reference this node
for (const [sourceNode, connections] of Object.entries(workflow.connections)) {
    if (connections.main) {
        connections.main.forEach(outputConnections => {
            if (outputConnections) {
                outputConnections.forEach(conn => {
                    if (conn.node === oldName) {
                        conn.node = newName;
                        console.log(`Updated connection from ${sourceNode} to ${newName}`);
                    }
                });
            }
        });
    }
}

// Rename the connection key if it exists
if (workflow.connections[oldName]) {
    workflow.connections[newName] = workflow.connections[oldName];
    delete workflow.connections[oldName];
    console.log(`Renamed connection key from "${oldName}" to "${newName}"`);
}

// Write back the workflow
fs.writeFileSync(workflowPath, JSON.stringify(workflow, null, 2));
console.log('Workflow updated successfully');
