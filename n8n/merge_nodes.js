const fs = require('fs');
const path = require('path');

const workflowPath = path.join(__dirname, 'workflow', 'SoccerLST.json');
const normalizeCodePath = path.join(__dirname, 'codenode', 'Normalize previous output.js');

// Read workflow and code file
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));
const codeContent = fs.readFileSync(normalizeCodePath, 'utf8');

// Extract just the code (remove header comment)
const codeLines = codeContent.split('\n');
const codeStartIndex = codeLines.findIndex(line => line.trim() === '*/') + 1;
const cleanCode = codeLines.slice(codeStartIndex + 1).join('\n');

// Find and update the "Normalize previous output" node
const normalizeNodeIndex = workflow.nodes.findIndex(node => node.name === 'Normalize previous output');
if (normalizeNodeIndex !== -1) {
    workflow.nodes[normalizeNodeIndex].parameters.jsCode = cleanCode;
    console.log('Updated Normalize previous output node with merged code');
}

// Find the "Filter 10AM-12PM" node to remove
const filterNodeIndex = workflow.nodes.findIndex(node => node.name === 'Filter 10AM-12PM');
const filterNodeId = workflow.nodes[filterNodeIndex]?.id;

if (filterNodeIndex !== -1 && filterNodeId) {
    // Update connections to bypass the filter node
    // get_previous_mismatch -> Filter 10AM-12PM -> Normalize previous output
    // Should become: get_previous_mismatch -> Normalize previous output

    const getPreviousMismatchNode = workflow.nodes.find(node => node.name === 'get_previous_mismatch');
    if (getPreviousMismatchNode && workflow.connections['get_previous_mismatch']) {
        // Update connection to point directly to Normalize previous output
        workflow.connections['get_previous_mismatch'].main[0] = [
            {
                node: 'Normalize previous output',
                type: 'main',
                index: 0
            }
        ];
        console.log('Updated connections to bypass Filter 10AM-12PM');
    }

    // Remove the Filter 10AM-12PM connection
    delete workflow.connections['Filter 10AM-12PM'];

    // Remove the Filter 10AM-12PM node
    workflow.nodes.splice(filterNodeIndex, 1);
    console.log('Removed Filter 10AM-12PM node from workflow');
}

// Write back the workflow
fs.writeFileSync(workflowPath, JSON.stringify(workflow, null, 2));
console.log('Workflow updated successfully');
