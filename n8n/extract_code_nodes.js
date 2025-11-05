const fs = require('fs');
const path = require('path');

// Read the workflow JSON
const workflowPath = path.join(__dirname, 'workflow', 'Soccer_LST_Parse_Excel_File.json');
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Map of node names to their code files
const codeNodeMap = {
  'format_working_shift': 'format_working_shift.js',
  'wrap_working_shift': 'wrap_working_shift.js',
  'enrich_assinged_job': 'enrich_assinged_job.js',
  'format_trader_list': 'format_trader_list.js',
  'filter_and_prepare_previous_mismatch': 'filter_and_prepare_previous_mismatch.js',
  'wrap_previous_mismatch': 'wrap_previous_mismatch.js',
  'format_mismatch': 'format_mismatch.js',
  'prepare_current_mismatch': 'prepare_current_mismatch.js',
  'wrap_current_mismatch': 'wrap_current_mismatch.js',
  'format_config': 'format_config.js',
  'format_pair_trader': 'format_pair_trader.js',
  'enrich_assigned_jobs_with_pointed_trader': 'enrich_assigned_jobs_with_pointed_trader.js'
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

console.log('\nCode extraction completed successfully!');
