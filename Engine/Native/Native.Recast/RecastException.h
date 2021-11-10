#pragma once

ref class RecastException :	public System::Exception
{
public:
	RecastException(System::String ^text) : System::Exception(text)
	{
	}
};

