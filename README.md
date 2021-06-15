# IronStar

## Checkout and build instructions

  1. Download and install [GitHub Desktop](https://desktop.github.com/). LFS must be installed!
  2. Download and install [Visual Studio 2015 Community Edition](https://www.visualstudio.com/post-download-vs/?sku=community&clcid=0x409&downloadrename=true&__hstc=268264337.0e64c25d2dac26ca9c64c14163a399c9.1478012092918.1478012092918.1478012092918.1&__hssc=268264337.1.1478012092918&__hsfp=3200057308#). The following features must be installed:
     1. Programming Languages -> Visual C++ -> Common Tools for Visual Studio 2015
     2. Windows and Web Development -> Windows 8.1 and Windows Phone 8.0/8.1 Tools
     3. Windows and Web Development -> Windows 8.1 and Windows Phone 8.0/8.1 Tools -> Tools and Windows SDKs
  3. _Download and install [DirectX Redist Jun 2010]_ (http://download.microsoft.com/download/8/4/A/84A35BF1-DAFE-4AE8-82AF-AD2AE20B6B14/directx_Jun2010_redist.exe)
  5. _Make sure that DirectX Redist was successfully installed._ 
  4. ~~Read paragraph `3` again. It's really crucial!~~ You may skip p.3 and try to install DirectX Redist Jun 2010 later if something goes wrong.
  6. Checkout https://github.com/demiurghg/IronStar.git. 
     It's better to use GitHub Desktop: `git clone https://github.com/demiurghg/IronStar.git`
  7. Wait. Checkout will take about two minutes.
  8. Do not forget to initialize LFS.
  9. Open `IronStar.sln`.
  10. Set active solution configuration to `Release`, because even debug configuration uses release version of tools.
  11. Go to Configuration Manager and make sure that all projects are included to build.
  12. Build.
  13. Make project `IronStar` as StartUp Project.
  14. Run project `IronStar`. First launch will take several minutes to compile content and will show some warnings.
  15. Push `Launch`.
  16. ???
  17. Profit???
  18. Yes?
  19. No!
  20. Run Editor, open `FirstLevelDraft`.
  21. Push `Bake Lightmap` and `Capture LightProbes`.
  22. Exit editor.
  23. Run game, select `FirstLevelDraft`.
  24. ...
  25. Profit???
  26. YES!!! 

## Common problems and solutions
  * **Problem**: I can not even build the project: `error MSB3103: Invalid Resx file. Type in the data at line 123, position 5, cannot be loaded because it threw the following exception during construction: Parameter is not valid.`
  * **Solution**: Make sure that GIT LFS is installed.
  
  * **Problem**: The program or feature C:\IronStar\SDKs\nvcompress.exe cannot start or run due to incompatibility with x64 version of Windows.
  * **Solution**: Make sure that GIT LFS is installed.
  
  * **Problem**: particles.hlsl: bad combination: 0x00000008: INITIALIZE
  * **Solution**: Make sure that Tools and Windows SDKs are installed.

  * **Problem**: Failed to load Microsoft.ConcurrencyVisualizer.Markers
  * **Solution**: In case of "Strong name validation failed", open the command prompt as administrator and enter following commands:
    1. `reg DELETE "HKLM\Software\Microsoft\StrongName\Verification" /f`
    2. `reg ADD "HKLM\Software\Microsoft\StrongName\Verification\*,*" /f`
    3. `reg DELETE "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification" /f`
    4. `reg ADD "HKLM\Software\Wow6432Node\Microsoft\StrongName\Verification\*,*" /f`

