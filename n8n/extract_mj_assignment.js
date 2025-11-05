const fs = require('fs');
const path = require('path');

// Read the workflow JSON
const workflowPath = path.join(__dirname, 'workflow', 'Soccer_LST_MJ_Assignment.json');
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Map of node names to their code files
const codeNodeMap = {
  'enrich_assigned_jobs_with_pointed_trader': 'enrich_assigned_jobs_with_pointed_trader.js',
  'AI_detect_league_code': 'AI_detect_league_code.js',
  'build_mj_api_param': 'build_mj_api_param.js'
};

// Extract code from each node and write to files
for (const node of workflow.nodes) {
  if (codeNodeMap[node.name]) {
    const codeFilePath = path.join(__dirname, 'codenode', codeNodeMap[node.name]);
    
    // Get the jsCode from the node
    if (node.parameters && node.parameters.jsCode) {
      const codeContent = node.parameters.jsCode;
      
      // Write to the code file
      fs.writeFileSync(codeFilePath, codeContent, 'utf8');
      
      console.log(`Extracted code from node: ${node.name} -> ${codeNodeMap[node.name]}`);
    } else {
      console.log(`Warning: No jsCode found for node: ${node.name}`);
    }
  }
}

console.log('\n✅ Code extraction from Soccer_LST_MJ_Assignment completed successfully!');

