using FFBatchConverter;
using Terminal.Gui;

Application.Init();

try
{
	Application.Run(new TopLevelView());
}
finally
{
	Application.Shutdown();
}
