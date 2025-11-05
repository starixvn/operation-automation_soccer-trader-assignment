const fs = require('fs');
const path = require('path');

// Read the workflow JSON
const workflowPath = path.join(__dirname, 'workflow', 'Soccer_LST_Parse_Excel_File.json');
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Map of node names to their code files
const codeNodeMap = {
  'format_working_shift': 'ExcelParser/format_working_shift.js',
  'wrap_working_shift': 'ExcelParser/wrap_working_shift.js',
  'enrich_assinged_job': 'ExcelParser/enrich_assinged_job.js',
  'format_trader_list': 'ExcelParser/format_trader_list.js',
  'filter_and_prepare_previous_mismatch': 'ExcelParser/filter_and_prepare_previous_mismatch.js',
  'format_mismatch': 'ExcelParser/format_mismatch.js',
  'prepare_current_mismatch': 'ExcelParser/prepare_current_mismatch.js',
  'format_config': 'ExcelParser/format_config.js',
  'format_pair_trader': 'ExcelParser/format_pair_trader.js'
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
console.log('\nWorkflow updated successfully!');
