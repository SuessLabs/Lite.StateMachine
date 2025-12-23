@echo off

REM NOTE:
REM   The following is a sample template for "MyProject"
REM   This does not run, you MUST modify it to suit your needs.
REM   Sample Assumptions:
REM     1. Your project built to the folder, "output\MyProject\bin\Debug\"
REM     2. Your project accepts parameter, `-statediagrams` to ExportUml()

REM Navigate to output directory (was, 'output')
del /Q /S "output\\DotGraphs\*"
cd output\MyProject\bin\Debug\

echo Removing old graphs from DotGraphs folder...
del /Q /S DotGraphs\*
REM del /Q /S output\DotGraphs\*

REM
echo Generating...
MyProject.exe -statediagrams

REM cd ..\..\..\..
REM mkdir output\DotGraphs

REM :: Graphviz Resources:
REM :: https://graphviz.org/resources/#c-and-net
REM :: https://www.nuget.org/packages/QuikGraph.Graphviz (actively maintained)

echo Installing Graphviz...
nuget install Graphviz -Version 2.38.0.2 -OutputDirectory %userprofile%\\.nuget\\packages\\

SET dotgraph="%userprofile%\\.nuget\\packages\\graphviz\\2.38.0.2\\dot.exe"

echo Current Directory:
CD
echo .

IF EXIST "%userprofile%\\.nuget\\packages\\graphviz\\2.38.0.2\\dot.exe" (
  echo Using NuGets from UserProfile...
  REM for /f %%f in ('dir /b output\\DotGraphs\\*.dot') do %dotgraph% -Tpng -o output\\DotGraphs\\%%~nf.png output\\DotGraphs\\%%f
  for /f %%f in ('dir /b DotGraphs\\*.dot') do (
    echo + Generating PNG for file: %%f
    echo + DotGraph location: %dotgraph%
    %dotgraph% -Tpng -o DotGraphs\\%%~nf.png DotGraphs\\%%f
  )
) ELSE (
  IF EXIST "packages\\Graphviz.2.38.0.2\\dot.exe" (
    echo Using NuGets from local folder...
    REM for /f %%f in ('dir /b output\\DotGraphs\\*.dot') do packages\\Graphviz.2.38.0.2\\dot.exe -Tpng -o output\\DotGraphs\\%%~nf.png output\\DotGraphs\\%%f
    for /f %%f in ('dir /b DotGraphs\\*.dot') do packages\\Graphviz.2.38.0.2\\dot.exe -Tpng -o DotGraphs\\%%~nf.png DotGraphs\\%%f
  ) ELSE (
    echo Graphvis could not be found
  )
)

REM Make files more accessable and go back to root folder
xcopy /S /Y "DotGraphs" "..\\..\\..\\..\\output\\DotGraphs\"
cd ..\..\..\..
