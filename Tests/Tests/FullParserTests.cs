using IncludeToolbox;

namespace Tests
{
    internal class FullParserTests
    {
        private const string V = "#pragma once\n\n#include <App/Process.h>\n#include <App/ComponentType.h>\n#include <App/PropertyPlacement.h>\n#include <App/RelNests.h>\n#include <App/ScheduleTimeControl.h>\n#include <App/TaskColorType.h>\n\n#include <QDateTime>\n\nnamespace App\n{\nclass Resource;\nclass Document;\nclass ObjectFactory;\n\n// TaskDefinitionStructure used for conversion from older version\nstruct TaskDefinition\n{\n    Base::String oldPropHide;\n    Base::String oldPropShow;\n};\n\nclass LX_APP_EXPORT Task : public App::Process\n{\npublic:\n    enum TaskTypeEnum  // Only helper enum, is not used for property\n    {\n        DAY,\n        WEEK,\n        WEEK5,\n        WEEK2DR,\n        HOUR,\n    };\n    enum MyVisibilityState\n    {\n        Invisible,\n        Mixed,\n        Visible,\n        Undefined\n    };\n\n    typedef App::Process inherited;\n    friend class Task_Factory;\n    friend struct TaskSort;\n\n    TYPESYSTEM_HEADER();\n    LX_NODE_HEADER();\n\npublic:\n    // CmdCreateTaskItems(TargetTask, Operation - add/Insert)\t// Create Task object + ScheduleTimeControl object + all relations objects\n    // CmdAttach2Task(App::Task * task, App::Element * elem);\t// Create/Remove relations between Object(Elements) and Task\n\n#pragma region IFC Definition\n    Core::PropertyText TaskID;\n    Core::PropertyText Status;\n    Core::PropertyText WorkMethod;\n    Core::PropertyBoolean IsMilestone;\n    Core::PropertyInteger Priority;\n\n    // App::Task * getNestsObject();\t\t\t\t\t\t\t\t\t\t\t//  \tdecomposes_  SET[0:1]\t// Restrict the relationship \'Nests\' (decomposes_) inherited from Object\n    // to Task. std::unordered_set<App::Task *> getIsNestedByObjects();\t\t\t\t//\t \tisDecomposedBy_\t \t\t// Restrict the relationship\n    // \'IsNestedBy\' (isDecomposedBy_) inherited from Object to Task. Base::String getName() { return userName.getValue(); }\t\t\t\t//\n    // userName\t\t\t\t// The Name attribute should be inserted to describe the task name.\n#pragma endregion IFC Definition\n\n    // App::Task is attached to App::ScheduleTimeControl\n    // Create App::RelAssignsTasks if does not exist and set links and back links\n    App::RelAssignsTasks* AttachScheduleTimeControl(App::ScheduleTimeControl* schedule, App::RelAssignsTasks* rel = NULL);\n    // Clear links and backlinks and return relation for cmd or dispose purposes\n    App::RelAssignsTasks* DetachScheduleTimeControl();\n    // ScheduleTimeControl is stored in ObjectDefinition.hasAssignments_ inside App::RelAssignsTasks as RelatingControl, but I use Internal pointer to\n    // save finding\n    App::ScheduleTimeControl* GetScheduleTimeControl()\n    {\n        return _attachedScheduleTimeControl;\n    }\n    // Create App::RelNests and set links and back links\n    App::RelNests* AttachParentTask(App::Task* parent, App::RelNests* rel = NULL);\n    // Clear links and backlinks return App::RelNests for Cmd or dispose purposes\n    App::RelNests* DetachParentTask();\n\n    bool ContainParent(App::Task* parent);\n    App::Task* GetParentTask();\n    int GetLevelOfNested();\n    // App::Task * GetTopParentTask();\n    bool HasChildren();\n    bool ContainsChildTaskInTree(App::Task* child);\n    App::Task* GetFirstChildTask();\n    App::Task* GetLastChildTask();\n    App::Task* GetLastLeafChildTask();\n    int GetCountOfAllChildren();\n    // Children are sorted according App::RelSequence\n    inline std::vector<App::Task*> GetChildTasks()\n    {\n        return _soretedChildren;\n    }\n    inline std::set<QString> GetMultigeoGuId()\n    {\n        return _multigeoGuId;\n    }\n    inline void SetMultigeoGuId(std::set<QString> setVal)\n    {\n        _multigeoGuId = setVal;\n    }\n\n    App::RelSequence* AttachPredecesorTask(App::Task* task, App::RelSequence* rel = NULL);  // Create App::RelSequence and set links and back links\n    App::RelSequence* DetachPredecesorTask();  // unset links and back links return App::RelSequence\t// for Cmd or dispose\n    App::RelSequence* AttachSucessorTask(App::Task* task, App::RelSequence* rel = NULL);  // Create App::RelSequence and set links and back links\n    App::RelSequence* DetachSucessorTask();  // unset links and back links return App::RelSequence\t// for Cmd or dispose\n\n    App::Task* GetPredecessorTask();\n    App::Task* GetSuccessorTask();\n    // Return FirstChild Task or Succesor Task or Succesor of Parent if this child does not have succesor\t(Return \"next line\" in tree)\n    App::Task* GetNextLineTaskInTree();\n    App::Task* GetPrevLineTaskInTree();\n\n    bool IsConnectedTo(App::Task* task);\n\n    std::set<App::Element*> GetRelatedElements();    // only App::Elements from App::Process.OperatesOn  // No childern\n    std::set<App::Resource*> GetRelatedResources();  // only App::Resource from App::Process.OperatesOn  // No children\n\n    std::set<App::Element*> GetAllRelatedElements();  // my RelatedElements + RelatedElements of my children recursively\n\n    App::RelAssignsToProcess* AttachResource(App::Resource* resTempl, int qua, App::RelAssignsToProcess* rel = NULL);\n    App::RelAssignsToProcess* DetachResource(App::Resource* resTempl)\n    {\n        return AttachResource(resTempl, 0);\n    }\n\n#pragma region Lexocad properties\n    App::Task::MyVisibilityState GetVisibility();\n    void SetExpanded(bool onoff)\n    {\n        expanded.setValue(onoff);\n    }\n    bool IsExpanded()\n    {\n        return expanded.getValue();\n    }\n    bool IsParentExpanded();\n    void SetTaskNumberWithoutTouch(int num);\n    void SetTaskNumber(float num, bool extended = false);\n    float GetTaskNumber(bool extended = false) const;\n\n    Core::PropertyInteger elementCount;\n    Core::PropertyBoolean demolition;\n    Core::PropertyBoolean subComponent;\n    Core::PropertyBoolean moveElement;\n    App::PropertyPlacementList originalElementsPlacement;\n    App::PropertyPlacementList newElementsPlacement;\n    App::PropertyPlacementMap originalElementsPlacementMap;\n    App::PropertyPlacementMap newElementsPlacementMap;\n\n\n    Core::PropertyLinkSetBase hideTasks;\n    Core::PropertyBoolean hideSelf;\n    Core::PropertyLinkSetBase showTasks;\n    Core::PropertyLink<App::TaskColorType*> taskColorType;  // SharedObject\n\n    Core::PropertyLink<App::ComponentType*> material;\n\n    Core::PropertyText UserProperty1;  // Km\n    Core::PropertyText UserProperty2;  // Scene\n    Core::PropertyText UserProperty3;  // eBKP\n\n    Core::PropertyText MSProjectID;\n    Core::PropertyIndex number2dr;\n#pragma endregion Lexocad properties\n\n\n    // void addAssignmentObject(App::ObjectDefinition * object, App::ObjectTypeEnum objectType = NOTDEFINED) override;\n\n    virtual Core::ExecuteStatus execute(Core::ExecuteContext* /*context*/) override\n    {\n        return Core::EXECUTE_OK;\n    }\n    void restore(Base::AbstractXMLReader& reader, Base::PersistenceVersion& version) override;\n\n    bool mustbeSaved() const override\n    {\n        return true;\n    }\n\n    App::Task* FindTaskNumber(int taskNum, App::Task* task = NULL);\n    App::Task* FindTaskNumberOrBigger(int taskNum, int originalNum, App::Task* task = NULL);\n    App::Task* FindTaskNumberOrSmaller(int taskNum, int originalNum, App::Task* task = NULL);\n    App::Task* FindTaskOnPosition(int offsetLine);\n\n    // static\t\tApp::Task * FindTaskNumberOrSmaller(int taskNum, App::Task * firstRootTask);\n\n    /// Returns task of the given number if exists, nullptr otherwise\n    static App::Task* getTaskByNumber(Core::CoreDocument* cdoc, int number);\n\n    void InitPrivateProperties();\n\n    // Not Saved temporary data, used for building Tree and GUI stuff\n    void* TreeItemPointer;        // for storing QStandardItem during buildng QStandardItemModel in TaskTreeView plugin\n    void* TreeSimpleItemPointer;  // for storing QStandardItem during buildng QTreeWidget in TaskTreeViewSimple plugin\n    QDateTime TmpStart;           // Tmp date for updating formula\n    QDateTime TmpEnd;             // Tmp date for updating formula\n    int MyCheckState;             // For MyCheckState used insted of QtCheckState, because Andreas wants different behavior/order of states\n\nprotected:\n    Task(void);\n    virtual ~Task(void);\n\n    Core::PropertyBoolean expanded;\n    Core::PropertyInteger taskNumber;\n\n    void restoreProperty(Core::Property* property,\n                         const Base::String& name,\n                         Base::AbstractXMLReader& reader,\n                         Base::PersistenceVersion& version) override;\n    void sortChildren();\n\nprivate:\n    TaskDefinition _newDefRestore;  // for converting from older version\n\n    // Store pointer to ScheduleTimeControl to save time by finding type in ObjectDefinition.hasAssignments_\n    App::ScheduleTimeControl* _attachedScheduleTimeControl;\n    // Store sorted child tasks vector to save time\n    std::vector<App::Task*> _soretedChildren;\n    // multigeo children list, needed for week2dr mode (hacks)\n    std::set<QString> _multigeoGuId;\n\n    App::Task* findTaskOnPosition(int offsetLine, int& counter);\n};\n\nDECLARE_OBJECT_FACTORY(Task_Factory, App::Task, IFCTASK)\n\n\n\nstruct TaskSort\n{\n    inline bool operator()(const App::Task* t1, const App::Task* t2)\n    {\n        if (t1->taskNumber.getValue() < t2->taskNumber.getValue())\n            return true;\n        else if (t1->taskNumber.getValue() > t2->taskNumber.getValue())\n            return false;\n        else\n            return t1 < t2;\n    }\n};\n}  // namespace App\n\n#ifndef SWIG\nQ_DECLARE_METATYPE(App::Task*)\n#endif";

        [Test]
        public void TestBasic()
        {
            var text = "#include <hell>";
            var lines = Parser.Parse(text, true);
            Assert.IsTrue(lines.Includes.Any());
            Assert.IsFalse(lines.Declarations.Any());
            Assert.IsFalse(lines.Namespaces.Any());

            var line = lines.Includes[0];
            Assert.That(text, Is.EqualTo(line.Project(text)));
        }
        [Test]
        public void TestBasic2()
        {
            var text = "#include <hell> \n";
            var lines = Parser.Parse(text, true);
            Assert.IsTrue(lines.Includes.Any());
            Assert.IsFalse(lines.Declarations.Any());
            Assert.IsFalse(lines.Namespaces.Any());

            var line = lines.Includes[0];
            Assert.That(line.Project(text), Is.EqualTo("#include <hell>"));
        }
        [Test]
        public void TestBasicNamespace()
        {
            var text = "namespace a{}";
            var lines = Parser.Parse(text);
            Assert.IsFalse(lines.Includes.Any());
            Assert.IsFalse(lines.Declarations.Any());
            Assert.IsTrue(lines.Namespaces.Any());

            var line = lines.Namespaces[0];
            Assert.That(line.namespaces[0], Is.EqualTo("a"));


            var text2 = "namespace {}";
            var lines2 = Parser.Parse(text2);
            Assert.IsFalse(lines2.Includes.Any());
            Assert.IsFalse(lines2.Declarations.Any());
            Assert.IsFalse(lines2.Namespaces.Any());
        }
        [Test]
        public void TestBasicFWDDecl()
        {
            var text = "namespace a{class b;\r\n}";
            var lines = Parser.Parse(text);
            Assert.IsFalse(lines.Includes.Any());
            Assert.IsTrue(lines.Declarations.Any());
            Assert.IsTrue(lines.Namespaces.Any());

            var line = lines.Namespaces[0];
            Assert.That(line.namespaces[0], Is.EqualTo("a"));

            var decl = lines.Declarations[0];
            Assert.That(decl.namespaces[0], Is.EqualTo("a"));
            Assert.That(decl.ID, Is.EqualTo("b"));
            Assert.That(text.Substring(decl.span.Start, decl.span.Length), Is.EqualTo("class b;"));
        }
        [Test]
        public void TestRealistic()
        {
            var text = V;
            var lines = Parser.Parse(text);
            Assert.IsTrue(lines.Includes.Any());
            Assert.IsTrue(lines.Includes.Count == 7);
            Assert.IsTrue(lines.Declarations.Any());
            Assert.IsTrue(lines.Declarations.Count == 3);
            Assert.IsTrue(lines.Namespaces.Any());
        }
    }
}
