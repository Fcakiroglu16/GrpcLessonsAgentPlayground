## Communication
- Always respond in English.

## Kubernetes Deployment Workflow
When deploying Kubernetes resources, follow this sequential workflow:
1. **First**: Run the `k8s-deploy-validator` agent to deploy and validate resources
2. **Second**: Run the `k8s-log-analyzer` agent to analyze pod logs for errors and warnings

This ensures proper deployment validation before log analysis.


## MCP Access
- This agent has access to all available MCP (Model Context Protocol) servers.
- All sub-agents inherit MCP access from the orchestrator.
- Available MCPs: Azure MCP, Microsoft Search MCP, and any other MCPs configured in the workspace.