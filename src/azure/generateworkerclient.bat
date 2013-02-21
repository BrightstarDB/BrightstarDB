svcutil NetworkedPlanet.Brightstar.Azure.StoreWorker\bin\debug\NetworkedPlanet.Brightstar.Azure.StoreWorker.dll
svcutil *.wsdl *.xsd /language:C# /out:NetworkedPlanet.Brightstar.Azure.StoreWorkerClient/StoreWorkerClient.cs /namespace:*,NetworkedPlanet.Brightstar.Azure.StoreWorkerClient /reference:NetworkedPlanet.Brightstar.Azure.Common/bin/debug/NetworkedPlanet.Brightstar.Azure.Common.dll
REM svcutil *.wsdl *.xsd /language:C# /out:NetworkedPlanet.Brightstar.Azure.StoreWorker/WorkerClient.cs /namespace:*,NetworkedPlanet.Brightstar.Azure.StoreWorker.Client /reference:NetworkedPlanet.Brightstar.Azure.Common\bin\debug\NetworkedPlanet.Brightstar.Azure.Common.dll /internal
REM del *.wsdl
REM del *.xsd
REM del output.config