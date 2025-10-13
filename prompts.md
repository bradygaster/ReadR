Use the Azure MCP Tools (invoke best-practices first) to find the one Azure AI Foundry resource in my active subscription. If there's no active Azure subscription, pick the one with my name in it and show me the name of the subscription, but please do not print my subscription id. For the selected resource, return a JSON array entry with: { "subscriptionName", "resourceName", "resourceGroup", "location", "openaiFriendlyEndpoint", "models": [ { "modelName", "deploymentName" } ] }. After listing results, ask for confirmation before running `dotnet user-secrets` in project `ReadR.Frontend` to set `AZURE_OPENAI_ENDPOINT` to the OpenAI-friendly endpoint and `AZURE_OPENAI_MODEL_NAME` to my `gpt` deployment. Do not set secrets or print the subscription id without my confirmation.

Can you use the Azure MCP tools to find my active Azure subscription? If there isn't one selected, pick the one with my name in it.

Can you use the Azure MCP tools to iterate over all the resources and their resource types in my Azure subscription?

Use the Azure MCP Tools to find the single Azure AI Foundry resource in my active subscription (resource type: microsoft.cognitiveservices/accounts). If there is only one, return its name, resource group, location, and OpenAI-friendly endpoint. Also list all deployed models and their deployment names. Do not print the subscription id. If no resource is found, tell me. After listing, ask for confirmation before setting any secrets.

Can you use the foundry_openai_models-list tool to get all the model deployments in my AI Foundry instance ca-foundry-g4m326f3 and also give me back its OpenAI-friendly endpoint?

Use the Azure MCP Tools to find my active Azure subscription. If there isn't one selected, pick the one with my name in it. 

Use the Azure MCP Tools to find the single Azure AI Foundry resource in my active subscription (resource type: microsoft.cognitiveservices/accounts). If there is only one, return its name, resource group, location, and OpenAI-friendly endpoint. After listing, ask for confirmation before setting any secrets or if you need values for any you can't figure out. 

Run the `dotnet user-secrets` command in the `ReadR.Frontend` project directory to set  `AZURE_OPENAI_ENDPOINT` to the OpenAI-friendly endpoint and `AZURE_OPENAI_MODEL_NAME` to `gpt-4.1`.
