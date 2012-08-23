ClickTest
=========

Automatic regressiontesting of applications / Coded UI Tests.

This library has been developed to allow developers and testers to write automated regression-tests in a nice and readable syntax. We think the syntax is only a "bit" more detailed than regular manual test-scripts.
As of now the library is made to support applications reachable through MS UI Automation. So far we've only developed and tested it with a WPF application, but we hope it will be useful for other types of applications as well.
The code that hides away all the code needed to use MS UI Automation, is in the classes under AutomationCode and DslObjects.

The library is available as a Visual Studio 2010 project-file to be dropped into your solution and serve as a base library for your ClickTesting command-line/unit test-project. 
The library is also available as a NuGet package: http://nuget.org/packages/UiClickTestDSL/

### External libraries
The library uses TestApi from CodePlex (http://testapi.codeplex.com/) to do some of the mouse-movement and interaction with applications.

### Samples
We have provided a couple of samples of how we use this library:
SampleSpecializedUiTestDsl.cs: Is where we put all code that are spesific to testing our application
SampleProgram.cs: The class used as an executable when not running in a unit testrunner. Also contains the AssemblyInitialize for the unit tests.
SampleTestClass.cs: A few basic tests that show how the library can be used.

These classes and all our tests are placed in a project which references UiClickTestDsl. The project contains unit tests that can be run in Visual Studio, and it compiles to an executable that we run on our test computer.
Note: We have this in a separate solution, since these tests does not allow us to use the computer while they are running.

### More information
http://einplassskrivenedstuff.blogspot.no/2012/06/click-testing-or-automatic-regression.html

http://einplassskrivenedstuff.blogspot.no/2012/07/overview-of-how-to-use-click-test-dsl.html

http://einplassskrivenedstuff.blogspot.no/2012/07/overview-of-how-click-test-dsl-is.html