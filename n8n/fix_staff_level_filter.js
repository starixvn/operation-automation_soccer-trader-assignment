const fs = require('fs');
const path = require('path');

const workflowPath = path.join(__dirname, 'workflow', 'SoccerLST.json');

// Read workflow
const workflow = JSON.parse(fs.readFileSync(workflowPath, 'utf8'));

// Find the "Filter By Bettype Traders" node
const nodeIndex = workflow.nodes.findIndex(node => node.name === 'Filter By Bettype Traders');

if (nodeIndex !== -1) {
    const node = workflow.nodes[nodeIndex];
    const conditions = node.parameters.rules.values[0].conditions.conditions;

    // Update the staff level comparison values
    conditions.forEach(condition => {
        if (condition.id === 'mj-trader-elite' && condition.rightValue === 'Elite') {
            condition.rightValue = 'elite';
            console.log('Updated Elite -> elite');
        }
        if (condition.id === 'mj-trader-member' && condition.rightValue === 'Member') {
            condition.rightValue = 'member';
            console.log('Updated Member -> member');
        }
        if (condition.id === 'mj-trader-1st' && condition.rightValue === '(1st)') {
            condition.rightValue = 'first';
            console.log('Updated (1st) -> first');
        }
    });

    // Write back the workflow
    fs.writeFileSync(workflowPath, JSON.stringify(workflow, null, 2));
    console.log('Workflow updated successfully');
} else {
    console.log('Filter By Bettype Traders node not found');
}
