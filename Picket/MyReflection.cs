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
            rd.Properties = SetOptions(rd.iRandomInstance);

            prop = rd.iRandomInstance.GetType().GetProperty("ToolProperties");
            rd.ToolPropertiesObj = prop.GetValue(rd.iRandomInstance, null);

            ////rd.iRandomInstance.GetType().GetProperty("ToolProperties").GetValue(rd.iRandomInstance, null).GetType().GetProperty("TextToShow")
            rd.ToolProperties = SetToolProperties(rd.iRandomInstance);

            prop = rd.iRandomInstance.GetType().GetProperty("EntryList");
            rd.EntryList = prop.GetValue(rd.iRandomInstance, null);

            rd.EntryType = rd.Assembly.GetType("RandomTool.Entry");

            //-----------Actions----------
            EventInfo stopEventInfo = iRandomToolType.GetEvent("ToolStopCall");
            Type eventType = rd.Assembly.GetType("RandomTool.ToolStopEventHandler");
            Delegate eventHandler = Delegate.CreateDelegate(eventType, f, "EventStopCall");
            rd.ActionEvents.Add(stopEventInfo);
            rd.ActionDelegates.Add(eventHandler);
            stopEventInfo.AddEventHandler(rd.iRandomInstance, eventHandler);

            EventInfo actionEventInfo = iRandomToolType.GetEvent("ToolActionCall");
            eventType = rd.Assembly.GetType("RandomTool.ToolActionEventHandler");
            eventHandler = Delegate.CreateDelegate(eventType, f, "EventActionCall");
            rd.ActionEvents.Add(actionEventInfo);
            rd.ActionDelegates.Add(eventHandler);
            actionEventInfo.AddEventHandler(rd.iRandomInstance, eventHandler);
            //----------------------------

            return rd;
        }

        public class ReflectionData
        {
            public List<EventInfo> ActionEvents { get; set; } = new List<EventInfo>();
            public List<Delegate> ActionDelegates { get; set; } = new List<Delegate>();
            public Assembly Assembly { get; set; }
            public object iRandomInstance { get; set; }
            public object ToolPropertiesObj { get; set; }
            public ToolProperties ToolProperties { get; set; }
            public Methods Methods { get; set; }
            public Options Properties { get; set; }
            public Type EntryType { get; set; }
            public object EntryList { get; set; }
        }
        private ToolProperties SetToolProperties(object o)
        {
            ToolProperties properties = new ToolProperties();
            properties.ArrowPosition = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ArrowPosition");
            properties.currentArrowDirection = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("currentArrowDirection");
            properties.ArrowImage = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ArrowImage");
            properties.TextToShow = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("TextToShow");
            properties.ForceUniqueEntryColors = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ForceUniqueEntryColors");
            properties.LineColor = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("LineColor");
            properties.LineWidth = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("LineWidth");
            properties.TextFontFamily = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("TextFontFamily");
            properties.TextFontStyle = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("TextFontStyle");
            properties.TextColor = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("TextColor");
            properties.TextColorAuto = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("TextColorAuto");
            properties.ShadowVisible = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ShadowVisible");
            properties.ShadowColor = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ShadowColor");
            properties.ShadowPosition = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ShadowPosition");
            properties.ShadowLength = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("ShadowLength");
            properties.CenterVisible = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("CenterVisible");
            properties.CenterColor = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("CenterColor");
            properties.CenterSize = o.GetType().GetProperty("ToolProperties").GetValue(o).GetType().GetProperty("CenterSize");
            return properties;
        }

        public class ToolProperties
        {
            public PropertyInfo ArrowPosition { get; set; }
            public PropertyInfo currentArrowDirection { get; set; }
            public PropertyInfo ArrowImage { get; set; }
            public PropertyInfo TextToShow { get; set; }
            public PropertyInfo ForceUniqueEntryColors { get; set; }
            public PropertyInfo LineColor { get; set; }
            public PropertyInfo LineWidth { get; set; }
            public PropertyInfo TextFontFamily { get; set; }
            public PropertyInfo TextFontStyle { get; set; }
            public PropertyInfo TextColor { get; set; }
            public PropertyInfo TextColorAuto { get; set; }
            public PropertyInfo ShadowVisible { get; set; }
            public PropertyInfo ShadowColor { get; set; }
            public PropertyInfo ShadowPosition { get; set; }
            public PropertyInfo ShadowLength { get; set; }
            public PropertyInfo CenterVisible { get; set; }
            public PropertyInfo CenterColor { get; set; }
            public PropertyInfo CenterSize { get; set; }
        }
        private Options SetOptions(object o)
        {
            Options options = new Options();
            options.IsBusy = o.GetType().GetProperty("IsBusy");
            options.IsDisposed = o.GetType().GetProperty("IsDisposed");
            options.AllowExceptions = o.GetType().GetProperty("AllowExceptions");
            return options;

        }
        public class Options
        {
            public PropertyInfo IsDisposed { get; set; }
            public PropertyInfo IsBusy { get; set; }
            public PropertyInfo AllowExceptions { get; set; }
        }
        private Methods SetMethods(Type t)
        {
            Methods methods = new Methods();
            methods.Draw = t.GetMethod("Draw", new Type[] { typeof(int), typeof(int), typeof(int) });
            methods.Start = t.GetMethod("Start", new Type[] { typeof(int), typeof(int), typeof(int) });
            methods.EntryAdd = t.GetMethod("EntryAdd", new Type[] { ((Type)t.Assembly.GetType("RandomTool.Entry", false, true)) });
            methods.EntriesClear = t.GetMethod("EntriesClear", new Type[] { });
            methods.ShuffleEntries = t.GetMethod("ShuffleEntries", new Type[] { });
            methods.EntryList = t.GetMethod("EntryList", new Type[] { });
            methods.Stop = t.GetMethod("Stop", new Type[] { });
            methods.Refresh = t.GetMethod("Refresh", new Type[] { });
            methods.Dispose = t.GetMethod("Dispose", new Type[] { });
            methods.IsReadable = t.GetMethod("IsReadable", new Type[] { typeof(Color), typeof(Color) });
            return methods;
        }
        public class Methods
        {
            public MethodInfo Draw { get; set; }
            public MethodInfo Start { get; set; }
            public MethodInfo EntryAdd { get; set; }
            public MethodInfo EntriesClear { get; set; }
            public MethodInfo ShuffleEntries { get; set; }
            public MethodInfo EntryList { get; set; }
            public MethodInfo Stop { get; set; }
            public MethodInfo Refresh { get; set; }
            public MethodInfo Dispose { get; set; }
            public MethodInfo IsReadable { get; set; }
        }
    }
}
