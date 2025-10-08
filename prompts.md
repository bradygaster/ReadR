using the azure mcp tools, can you tell me which of my azure subscriptions is active? It should be the one with my name in it

can you use the azure mcp tools again to see if that subscription has any ai foundry resources in it?

can you use `dotnet user-secrets` to set the AZURE_OPENAI_ENDPOINT and AZURE_OPENAI_MODEL_NAME configuration settings so my code can use that foundry instance? i'll want to use the gpt-4o deployment i have in it. make sure to use the "OpenAI-friendly" AI Foundry endpoint, not the base endpoint for the project.



## These two together seem to get it 

Find the Azure AI Foundry resource in my subscription and tell me its name. Then, list all models deployed in that resource.

Use `dotnet user-secrets` to set the `AZURE_OPENAI_ENDPOINT` to the "OpenAI-friendly" AI Foundry endpoint. Set `AZURE_OPENAI_MODEL_NAME` to the gpt-4o deployment I have in it. 
