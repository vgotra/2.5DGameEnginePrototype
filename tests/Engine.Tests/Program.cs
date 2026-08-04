using Engine.Tests;

bool allPassed = SmokeTestRunner.RunAll();
Console.WriteLine(allPassed ? "Smoke tests passed" : "Smoke tests failed");
return allPassed ? 0 : 1;
