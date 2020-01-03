using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace Picket
{
    public class MyReflection
    {
        public ReflectionData Instance { get; private set; }
        public MyReflection(Form f, Assembly assembly)
        {
            Instance = ReflectionLoadSet(f, assembly);
        }

        private ReflectionData ReflectionLoadSet(Form f, Assembly assembly)
        {
            ReflectionData rd = new ReflectionData();
            rd.Assembly = assembly;
            PropertyInfo prop;

            Type iType = rd.Assembly.GetType("RandomTool.IRandomTool");
            Type iRandomToolType = (from t in rd.Assembly.GetExportedTypes()
                                    where !t.IsInterface && !t.IsAbstract
                                    where iType.IsAssignableFrom(t)
                                    select t).FirstOrDefault();
            rd.iRandomInstance = Activator.CreateInstance(iRandomToolType, new object[] { f });
            rd.Methods = SetMethods(iRandomToolType);

            prop = rd.iRandomInstance.GetType().GetProperty("ToolProperties");
            rd.toolProperties = prop.GetValue(rd.iRandomInstance, null);

            prop = rd.iRandomInstance.GetType().GetProperty("EntryList");
            rd.EntryList = prop.GetValue(rd.iRandomInstance, null);

            rd.EntryType = rd.Assembly.GetType("RandomTool.Entry");

            //-----------Actions----------
            EventInfo stopEventInfo = iRandomToolType.GetEvent("ToolStopCall");
            Type eventType = rd.Assembly.GetType("RandomTool.ToolStopEventHandler");
            Delegate eventHandler = Delegate.CreateDelegate(eventType, f, "EventStopCall");
            stopEventInfo.AddEventHandler(rd.iRandomInstance, eventHandler);

            EventInfo actionEventInfo = iRandomToolType.GetEvent("ToolActionCall");
            eventType = rd.Assembly.GetType("RandomTool.ToolActionEventHandler");
            eventHandler = Delegate.CreateDelegate(eventType, f, "EventActionCall");
            actionEventInfo.AddEventHandler(rd.iRandomInstance, eventHandler);
            //----------------------------

            return rd;
        }
        public class ReflectionData
        {
            public Assembly Assembly { get; set; }
            public object iRandomInstance { get; set; }
            public object toolProperties { get; set; }
            public Methods Methods { get; set; }
            public Type EntryType { get; set; }
            public object EntryList { get; set; }
        }
        private Methods SetMethods(Type t)
        {
            Methods m = new Methods();
            m.draw = t.GetMethod("Draw", new Type[] { typeof(int), typeof(int), typeof(int) });
            m.start = t.GetMethod("Start", new Type[] { typeof(int), typeof(int), typeof(int) });
            m.entryAdd = t.GetMethod("EntryAdd", new Type[] { ((Type)t.Assembly.GetType("RandomTool.Entry", false, true)) });
            m.entriesClear = t.GetMethod("EntriesClear", new Type[] { });
            m.shuffleEntries = t.GetMethod("ShuffleEntries", new Type[] { });
            m.entryList = t.GetMethod("EntryList", new Type[] { });
            m.stop = t.GetMethod("Stop", new Type[] { });
            m.refresh = t.GetMethod("Refresh", new Type[] { });
            m.dispose = t.GetMethod("Dispose", BindingFlags.NonPublic | BindingFlags.Instance, null, CallingConventions.Any, new Type[] { typeof(bool) }, null);
            m.isReadable = t.GetMethod("IsReadable", new Type[] { typeof(Color), typeof(Color) });
            return m;
        }
        public class Methods
        {
            public MethodInfo draw { get; set; }
            public MethodInfo start { get; set; }
            public MethodInfo entryAdd { get; set; }
            public MethodInfo entriesClear { get; set; }
            public MethodInfo shuffleEntries { get; set; }
            public MethodInfo entryList { get; set; }
            public MethodInfo stop { get; set; }
            public MethodInfo refresh { get; set; }
            public MethodInfo dispose { get; set; }
            public MethodInfo isReadable { get; set; }
        }
    }
}
