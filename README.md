# Include Toolbox 
[![Build](https://github.com/Agrael1/IncludeToolbox/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/Agrael1/IncludeToolbox/actions/workflows/main.yml)

**_Tools for managing C/C++ #includes: Formatting, sorting, exploring, pruning._**  

Include Toolbox consists of 4 different tools. All of them are only applicable to VC++ projects.

![](/art/iformat.png) **[Command]** Include Formatter  
![](/art/itrial.png) **[Command]** Trial and Error Include Removal  
![](/art/iwyu.png) **[Command]** [Include-What-You-Use](https://include-what-you-use.org/) Integration  
![](/art/AddPageGuides.png) **[Command]** Mapper module for IWYU  
![](/art/igraph.png)**[Tool Window]** ~~Include Graph~~

## Links

[Open VSIX Gallery 2019](https://www.vsixgallery.com/extension/IncludeToolbox2019.1431faa5-aa04-47af-8289-9d887e0696a4)

[Open VSIX Gallery 2022](https://www.vsixgallery.com/extension/IncludeToolbox2022.d3cba4fe-8d65-479b-8436-18d743ee7b53)

Marketplace 2019(tbd)

Marketplace 2022(tbd)

[Version History](/doc/changelog.md)

# Tools in Detail

## Include Formatter

![Include Format](/art/includeformatter.gif)

Select a group of includes, right click, select "Format Selected Includes"

The behavior of this command is controlled by various options which can be configured in _Tools>Options>Include Toolbox>Include Formatter_:

*   Formatting
    *   Delimiter Mode  
        Optionally change "" to <> or vice versa
    *   Slash Mode  
        Optionally changes / to \ or vice versa
    *   Remove Empty Lines 
        Optionally removes empty lines within the selection
        
*   Path Reformatting
    *   Ignore File Relative  
        If true, the local file path will not be considered for reformatting the path
    *   Mode  
        Configures the strategy that should be used to determine new include paths
*   Sorting  
    The tool will always sort all selected includes alphabeticaly, unless..
    *   Precedence Regex  
        Every line gives a regex - if an include matches a regex, it has precedence over all other includes that do not match any, or a later regex. Multiple includes that match the same regex are still alphabetically sorted.
    *   Sort by Include Type  
        Optionally puts all inclues with either quotes or angle brackets first.
    *   Remove duplicates  
        Removes duplicate headers. May be suppressed using `//IWYU pragma: keep` e.g. for maintaining strong ordering dependency

All operations are performed in the order in which they occur on the option page.

## Trial and Error Include Removal

The name says it all: This tool will try to remove an include, recompile, see if it works and proceed to the next one accordingly.  
The tool can be started an all compilable files in a VC++ by right clicking on the code window. There is also a special version in the Project context menu which will run over every single compilable file in the project (takes very long).

Obviously the results of this tool are far from optimal and the process can take a while.

The exact behavior of this command can be controlled in *Tools>Options>Include Toolbox>Trial and Error Include Removal*:

*   Ignore List  
    A list of regexes. If the content of an include matches any of these, it will never be removed.
*   Ignore First Include  
    If true the top most include will always be ignored, does not work in headers
*   Removal Order  
    Wheater the tool should run from top to bottom or bottom to top (this can make a difference on the end result)

To suppress removal of a single include, add a comment to its line containing `//IWYU pragma: keep`

Since 3.2.47 works for header files as well.

## Include-What-You-Use Integration


![Include What You Use](/art/iwyu.gif)

Include Toolbox with an integration of the free [Include-What-You-Use](https://github.com/include-what-you-use/include-what-you-use). By default (see _Tools>Options>Include Toolbox>Include-What-You-Use_) it is downloaded together with a VC++ specific mapping file from [this github repository](https://github.com/Agrael1/BuildIWYU) upon first use (and whenever there is a newer version available in this repository). New version is automatically built and shipped every month.


Again, it can be activated by right clicking on a C++ Code file in a VC++ document. The Option page exposes most of IWYU's command line options and provides the option to directly apply the results. The complete output will be displayed in the Include Toolbox output window.

IWYU often does not work as expected - for more information look at the [official docs](https://github.com/include-what-you-use/include-what-you-use/tree/master/docs).

IWYU has several pragmas, described at [Pragmas](https://github.com/include-what-you-use/include-what-you-use/blob/master/docs/IWYUPragmas.md), e.g. `//IWYU pragma: keep` works as include removal suppresor.

Since 3.0.0:
Added mapper support. Maps produced with it are used to make results better, as it describes all include files within mapped file.

Added cheap and precise modes: cheap mode copies contents of IWYU output, may be undesirable, as it does not account forward declarations, but it is fast. Presice mode uses custom LL1 partial parser, which reads all the information from file and output, combining all the possibilities it allows for additional steps:
 - Format all includes
 - Extract all forward declarations and place them before code
 - Empty namespaces removal, useful combining with previous option 

There is a BETA feature of IWYU usage with several files:
 - Select several files in project menu.
 - *Right click>Run Include-What-You-Use*

It is useful for example with several .cpp files, when you are sure, that headers included are fully correct.

## Map Generator for Include-What-You-Use [beta]

The feature is tested, but it is useful even within large projects. It makes results of IWYU better. Works only on header files.

It gets all the #include declarations and writes them as they are to the specified mapping file. Combining several of those files are done using `{ref: }` in the final file. To find more visit [official mappings guide](https://github.com/include-what-you-use/include-what-you-use/blob/master/docs/IWYUMappings.md)

Configuration is on *Tools>Options>Include Toolbox>Include Mapper* page.

Mapper has one option, that specifies separator you would like to use, quotes or angle brackets. This option maps opposite choice as a private header, ultimately forsing IWYU to choose your vision of the file.

To specify relative index use *Relative File Prefix* option. e.g. C:\\users\\map\\a.h with prefix C:\\users will write <map/a.h> to the final map.

## ~~Include Graph~~

Added option page. Soon.
Requires fixes.

# FAQ:

*   Why don't you apply the formatting to all includes within a file?  
    This may sound desirable, but is very messy if there are optional includes (preprocessor) or specific exceptions where not all includes should be in the same place or in the default order.
*   XY didn't work, what is going on?  
    Look in the output window for Include Toolbox to get more information.
	
# Optimal Usage Pattern

1. Start from the header file in your project, that includes only standard library.
2. Use IWYU and/or TAEIR tool
3. Add file to mapper with pre-specified .imp file at *Tools>Options>Include Toolbox>Include Mapper*
4. Add this map file to the IWYU preset at *Tools>Options>Include Toolbox>Include-What-You-Use>Mapping file* 
5. Go through all the headers in your project, including them in mapper file 
6. After that go through all .cpp files with the same tools. IWYU has mass processing for several selected files. IThe best way of using this tool is in batches of \4-5 files. 
7. Compile files to asses the result.

# Final Words

The IWYU itself is far from perfect, TAEIR also, but combinig those tools and Mapping capabilities with other maps, provided by IWYU repo and defaults the results will be just good enough.