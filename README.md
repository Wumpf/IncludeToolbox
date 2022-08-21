# Include Toolbox 
[![Build](https://github.com/Agrael1/IncludeToolbox/actions/workflows/main.yml/badge.svg?branch=master)](https://github.com/Agrael1/IncludeToolbox/actions/workflows/main.yml)

**_Tools for managing C/C++ #includes: Formatting, sorting, exploring, pruning._**  

Include Toolbox consists of 4 different tools. All of them are only applicable to VC++ projects.

![](/art/iformat.png) **[Command]** Include Formatter  
![](/art/itrial.png) **[Command]** Trial and Error Include Removal  
![](/art/iwyu.png) **[Command]** [Include-What-You-Use](https://include-what-you-use.org/) Integration  
![](/art/AddPageGuides.png) **[Command]** Mapper module for IWYU  
![](/art/igraph.png)**[Tool Window]** ~~Include Graph~~

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

All operations are performed in the order in which they occur on the option page.

## Trial and Error Include Removal

The name says it all: This tool will try to remove an include, recompile, see if it works and proceed to the next one accordingly.  
The tool can be started an all compilable files in a VC++ by right clicking on the code window. There is also a special version in the Project context menu which will run over every single file in the project (takes very long).

Obviously the results of this tool are far from optimal and the process can take a while.

The exact behavior of this command can be controlled in _Tools>Options>Include Toolbox>Trial and Error Include Removal_:

*   Ignore List  
    A list of regexes. If the content of an include matches any of these, it will never be removed.
*   Ignore First Include  
    If true the top most include will always be ignored
*   Removal Order  
    Wheater the tool should run from top to bottom or bottom to top (this can make a difference on the end result)

To suppress removal of a single include, add a comment to its line containin_g_ _$include-toolbox-preserve$_

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

Requires fixes.

# FAQ:

*   Why don't you apply the formatting to all includes within a file?  
    This may sound desirable, but is very messy if there are optional includes (preprocessor) or specific exceptions where not all includes should be in the same place or in the default order.
*   XY didn't work, what is going on?  
    Look in the output window for Include Toolbox to get more information.

# Version History
* 3.1.22 
   * New Include Format parsing, performed using project Lexer
   * Small fixes and DTE reduction
* 3.0.0
   * Versions have new pattern (enforced by github pipelines) Major.Minor.Build, the build number does not decrease.
   * New SDK and Tools. General renewal. Visual Studio 2022 support, dropped support for 2015 and 2017.
   * Refactoring of IWYU, new code and new feature set.
   * Some features are dropped for now, until fixed. 
   * Build pipeline for IWYU, which builds every month at [Build Pipeline](https://github.com/Agrael1/BuildIWYU)!
   * CI/CD for this whole project!
   * Added Include mapper[beta] for IWYU, works as public-public include mapping.
   * Include What You Use additions:
       * Added LL1 partial parser for includes and forward declarations.
       * Added forward declaration moving to the beginning of the file, after all the includes.
       * Empty namespace removal tool.   
* 2.4.1
   * Fixed crash when opening context menu on some non-project files
* 2.4.0
   * Added support for Visual Studio 2019
   * Dropped support for Visual Studio 2015
   * Made some operations asynchronous under the hood, related bugfixing/checks driven by VS2019's static analysis warnings
* 2.3.0
   * Include Formatter contributions by  _[Dakota Hawkins](https://github.com/dakotahawkins)_
        *  has now a remove duplicates option which is enabled by default
        *  Fixed not adding newlines before the last line of a batch
   * Fixed TrialAndErrorRemoval stopping when encountering an unsupported document, changed operation timeout to a couple of minutes ([PR by _bytefactory73_](https://github.com/Wumpf/IncludeToolbox/pull/58))
  * Fixed IWYU failing for long command line argument ([PR by _codingdave_](https://github.com/Wumpf/IncludeToolbox/pull/60))
  * Trying now to query NMake settings for include paths if there is no VCCLCompilerTool present (happens if vcxproj is not a standard C++ project)
*   2.2.0
    *   IWYU Integration/Trial and Error Include Removal
        *   Introduced comment-tag to avoid removing include (thx to [_ergins23_ for suggesting](https://github.com/Wumpf/IncludeToolbox/issues/38))
    *   IWYU Integration
        *   Passes now arch parameter for x64 projects on (thx to [_Fei_ for reporting](https://github.com/Wumpf/IncludeToolbox/issues/43))
        *   Added option for custom parameters (thx to [_Fei_ for suggesting](https://github.com/Wumpf/IncludeToolbox/issues/44))
*   2.1.5
    *   [Fixed](https://github.com/Wumpf/IncludeToolbox/issues/41) random timeouts in Trial and Error Include Removal
    *   Updated internal library references & used VS Extension toolkit
*   2.1
    *   DGML graph saving feature improvements  

        *   Each nodes has information about child count and unique transitive child counts
        *   Option to color elements by transitive child count
        *   Option to group by folders, expanded or collapsed
        *   Messageprompt after graph is saved, allows to open in VS directly
    *   Other fixes and small improvements  

        *   Renamed "Try and Error Include Removal" to "_Trial_ and Error Include Removal" (thx to [_steronydh_ for reporting](https://github.com/Wumpf/IncludeToolbox/issues/35))
        *   Include sorting treats other preprocessor directives as barrier over which includes can't be moved (thx to [_etiennehebert_ for reporting](https://github.com/Wumpf/IncludeToolbox/issues/34))
        *   Pressing enter on item in Include Graph jumps to include (previously only double click)
        *   Fixed Include Graph not displaying graph when switching active file while graph is computed
*   2.0.1
    *   Fixed bug that BlankAfterRegexGroupMatch option would only work if RemoveEmptyLines was active as well.
    *   Fixed crash in formatter if delimiter mode not "Unchanged" + "Remove Empty Lines" was false. (thx to [_etiennehebert_ for reporting](https://github.com/Wumpf/IncludeToolbox/issues/33))
    *   Include Graph folder items end now in slashes.
*   2.0
    *   Rewrote Include Graph ("Include Viewer" previously)
        *   New, improved UI
        *   Allows to display includes grouped by folder
        *   Much faster graph bulid up using by direct parsing (as alternative to compile with /showIncludes)
        *   Double click can navigate to include site
        *   Graph can be saved as DGML file
    *   Trial-and-Error-Include-Removal "Ignore List" option does now support "$(currentFilename)" macro
        *   Default setting include "(\/|\\\\|^)$(currentFilename)\.(h|hpp|hxx|inl|c|cpp|cxx)$" to ignore corresponding header file in removal
*   1.8
    *   Include-what-you-use (iwyu):
        *   Iwyu.exe is no longer part of the package. Instead there is a automatic download and update from a [different repository](https://github.com/Wumpf/iwyu_for_vs_includetoolbox) on first use.
        *   iwyu.exe path can be configured by user
        *   In case of automatic download, mapping files in iwyu path will be added to configuration
        *   Fixed hardcoded defines being passed to iwyu
        *   MSVC version is correctly passed to iwyu
        *   Fixed issues with applying removal/addition of declarations
        *   Changes can now optionally run through IncludeFormatter (on by default)
    *   Formatter:
        *   Include parser recognizes all whitespace-only lines as empty
        *   No longer resolves includes via file local path if "Ignore File Relative" option is active
        *   Formatting applied to includes inside preprocessor conditionals again. (Still ignored for include removal though)
        *   Fixed incorrect include parse behavior for preceding /* */ comment.
        *   Fixed potential crashes in internal path resolve
    *   Other:
        *   New Icons!
        *   Safer against crashes in commands
        *   Codebase has now a handful of unit tests
*   1.7
    *   .inl and _inl.h are by default ignored for trial-and-error-include-removal (configurable)
    *   New option for trial-and-error-include-removal to keep line breaks (off by default)
    *   _Contributed_ by [Adam Skoglund](https://github.com/gulgi): Another fix for folder handling in trial-and-error-include-removal
*   1.6 _- _Contributed_ by [Adam Skoglund](https://github.com/gulgi)_  

    *   Basic support for #if/#endif  - any include within an #if/#endif block will be ignored.
    *   Better support for subdirectories in trial-and-error-include-removal on projects.
*   1.5
    *   Fixed problems with VCProject runtimes in VS2015 introduced in previous version.  
        Required suprisingly large internal restructuring to support both VS2015 and VS2017 equally.
*   1.4
    *   Support for VS2017
    *   "Format Selected Includes" action is now only visible if includes were actually selected.
    *   "Format Selected Includes" works partially now also on files that are not in the currently loaded project
    *   Fixed an error in IWYU include removal parsing
*   1.3 - __Contributed_ by [Dakota Hawkins](https://github.com/dakotahawkins)_  

    *   Added option to put spaces between precedence regex matches.
    *   Improved regex sorting via "Schwartzian transform" (= grouping by regex order number before sorting).
*   1.2 _- Contributed by [Dakota Hawkins](https://github.com/dakotahawkins)_
    *   Added option to include delimiters in precedence regex to allow more advanced sorting (for a sample see [original pull request](https://github.com/Wumpf/IncludeToolbox/pull/4)).
*   1.1
    *   Remove dependency to ezEngine.
    *   IncludeViewer visualizes now the output of the /showIncludes command instead of trying to run the preprocessor manually.
*   1.01
    *   Have includes with quotes or angle brackets first
*   1.0
    *   First release.
    *   Merged two old projects "Include Viewer" and "Include Formatter" to new "Include Toolbox" bundle
