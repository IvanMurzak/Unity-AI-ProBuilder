# Error Handling

Common error messages are centralized in the `Tool_ProBuilder.Error` static class (`ProBuilder.cs`). Tools throw `Exception` with these messages; the MCP framework serializes them back to the AI client.
