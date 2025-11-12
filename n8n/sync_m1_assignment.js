const fs = require('fs');
const path = require('path');

// Read the workflow JSON
const workflowPath = path.join(__dirname, 'workflow', 'Soccer_LST_M1_Assignment.json');
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Map of node names to their code files
const codeNodeMap = {
  'M1_enrich_assigned_jobs_for_working_shift': 'M1/M1_enrich_assigned_jobs_for_working_shift.js',
  'build_m1_api_param': 'M1/build_m1_api_param.js'
};

// Update each code node
for (const node of workflow.nodes) {
  if (codeNodeMap[node.name]) {
    const codeFilePath = path.join(__dirname, 'codenode', codeNodeMap[node.name]);
    const codeContent = fs.readFileSync(codeFilePath, 'utf8');

    // Update the jsCode parameter
    if (node.parameters) {
      node.parameters.jsCode = codeContent;
    }

    console.log(`Updated node: ${node.name}`);
  }
}

// Write the updated workflow back to file
fs.writeFileSync(workflowPath, JSON.stringify(workflow, null, 2), 'utf8');
console.log('\n✅ Soccer_LST_M1_Assignment workflow updated successfully!');
