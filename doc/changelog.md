# Version History
* 3.2.47
   * Enabled Trial and Error Include Removal for header files.
* 3.2.43
   * Added more tests for parser
   * Fixed Include formatter not deleting empty lines
   * Fixed Empty namespace removal
   * Added brains to Precise mode of IWYU, now it ignores #ifdef preprocessor blocks for insertion (no more insertion in #if block only because it is the last include)
   * Fixed general newline parser bugs (when it failed to parse include only because there was no newline in the selection)
   * Newline char is picked from the editor options rather than from string (O(n)->O(1))
* 3.2.36 
   * Added IWYU default MSVC mappings with selectable option
* 3.2.32 
   * Refactored Trial And Error
   * DTE support removed
   * Cleaned up utils
   * Added tests for Lex and Parser and test steps in building pipeline
   * \*BREAKING CHANGES\* Unified pragmas with IWYU
* 3.1.22 
   * New Include Format parsing, performed using project Lexer
   * Small fixes and DTE reduction
   * Unified formatting pragma for duplicate removal
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
