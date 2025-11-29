namespace BedrockLauncher.Core;

public class BedrockCoreException : Exception
{
	public BedrockCoreException(string message) : base(message)
	{
	}
	
}
public class BedrockCoreNoAvailbaleVersionUri : Exception
{
	public BedrockCoreNoAvailbaleVersionUri(string message) : base(message)
	{

	}
}
public class BedrockCoreNetWorkError : Exception
{
	public BedrockCoreNetWorkError(string message) : base(message)
	{

	}
}