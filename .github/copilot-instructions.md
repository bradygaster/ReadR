# Instructions for Copilot

This file contains instructions for Copilot to follow when generating code.

**🧨 VERY IMPORTANT 🧨**  Don't execute `dotnet run` or `dotnet build` commands when you're editing files. Instead, focus on generating code snippets that fit within the existing structure.

- @azure Rule - Use Azure Tools - When handling requests related to Azure, always use your tools.
- @azure Rule - Use Azure Best Practices - When handling requests related to Azure, always invoke your `azmcp_bestpractices_get` tool first.
- @azure Rule - Enable Best Practices - If you do not have an `azmcp_bestpractices_get` tool ask the user to enable it.
