Directory structure:
└── EchoServer/
    ├── EchoServer.csproj
    ├── EchoServer.sln
    └── Program.cs

================================================
File: EchoServer.csproj
================================================
﻿<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

</Project>



================================================
File: EchoServer.sln
================================================
﻿
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.14.36511.14 d17.14
MinimumVisualStudioVersion = 10.0.40219.1
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "EchoServer", "EchoServer.csproj", "{C4578E7A-553F-DEB6-B069-5968538D0593}"
EndProject
Global
	GlobalSection(SolutionConfigurationPlatforms) = preSolution
		Debug|Any CPU = Debug|Any CPU
		Release|Any CPU = Release|Any CPU
	EndGlobalSection
	GlobalSection(ProjectConfigurationPlatforms) = postSolution
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Debug|Any CPU.Build.0 = Debug|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Release|Any CPU.ActiveCfg = Release|Any CPU
		{C4578E7A-553F-DEB6-B069-5968538D0593}.Release|Any CPU.Build.0 = Release|Any CPU
	EndGlobalSection
	GlobalSection(SolutionProperties) = preSolution
		HideSolutionNode = FALSE
	EndGlobalSection
	GlobalSection(ExtensibilityGlobals) = postSolution
		SolutionGuid = {4A8CEEEE-CA9D-4752-B28D-25FB3F8AB2AC}
	EndGlobalSection
EndGlobal



================================================
File: Program.cs
================================================
癤퓎sing System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Net;
using System.Net.Sockets;
using System.Text;


int pid = Process.GetCurrentProcess().Id;
Console.WriteLine($"Current Process ID: {pid}");

var meter = new Meter("SocketServer.Metrics", "1.0");
var connectionCounter = meter.CreateCounter<int>("socket_connections_total");
var messageCounter = meter.CreateCounter<int>("socket_messages_received_total"); 
var messageLengthHistogram = meter.CreateHistogram<int>("socket_message_length");

var server = new TcpListener(IPAddress.Any, 32452);
server.Start();
Console.WriteLine("Socket server running on port 32452...");

while (true)
{
    var client = await server.AcceptTcpClientAsync();
    connectionCounter.Add(1);
    Console.WriteLine("Client connected");

    _ = Task.Run(async () =>
    {
        using var stream = client.GetStream();
        var buffer = new byte[1024];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead <= 0) break;

            messageCounter.Add(1);
            messageLengthHistogram.Record(bytesRead);

            var received = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"Received: {received}");

            var response = Encoding.UTF8.GetBytes($"Echo: {received}");
            await stream.WriteAsync(response, 0, response.Length);
        }
    });
}





