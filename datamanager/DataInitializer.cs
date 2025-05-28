using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using data;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.XPath;

namespace datamanager
{
    public class DataInitializer
    {
        private string initFileName;

        public DataInitializer(string initFileName)
        {
            this.initFileName = initFileName;
        }

        public void Seed(Session session)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            LogManager.GetCurrentClassLogger().Info($"Загрузка данных");

            Dictionary<string, Object> variables = new Dictionary<string, Object>();
            UpdateObjectsOnDatabase(session, AppDomain.CurrentDomain.BaseDirectory + initFileName, variables, false);

            stopwatch.Stop();

            LogManager.GetCurrentClassLogger().Info($"Загрузка данных завершена {stopwatch.Elapsed}");
        }


        #region Создание и обновление объектов БД из инициализационного файла

        public static Type GetObjectTypeByName(string typeName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type item in assembly.GetTypes())
                {
                    if (item.FullName == typeName)
                        return item;
                }
            }

            return null;
        }

        public static object StatickFieldRetriver(string name)
        {
            int idx = name.LastIndexOf(".");
            if (idx == -1)
                throw new Exception("Field not found: " + name);
            String enumTypeName = name.Substring(0, idx);
            String enumField = name.Substring(idx + 1, name.Length - idx - 1);
            Type sysEnum = GetObjectTypeByName(enumTypeName);
            if (sysEnum == null)
                throw new Exception("Type not found: " + enumTypeName);
            FieldInfo fi = sysEnum.GetField(enumField);
            if (fi == null)
                throw new Exception("Field not found: " + enumField);

            return fi.GetValue(null);
        }

        public static MemberInfo GetMemberInfo(Type sourceType, string path)
        {
            if (sourceType == null || String.IsNullOrEmpty(path)) return null;

            Type tmpDataType = sourceType;
            PropertyInfo tmpProperty = null;

            foreach (String pn in path.Split('.'))
            {
                if (tmpDataType == null) return null;
                tmpProperty = tmpDataType.GetProperty(pn);
                if (tmpProperty != null)
                    tmpDataType = tmpProperty.PropertyType;

            }

            return tmpProperty;
        }

        public static void SetMemberInfo(object target, string path, object value)
        {
            if (target == null || string.IsNullOrEmpty(path)) return;

            Type tmpDataType = target.GetType();
            PropertyInfo tmpProperty = null;
            object tmpTarget = target;

            foreach (string pn in path.Split('.'))
            {
                if (tmpDataType == null) return;
                tmpProperty = tmpDataType.GetProperty(pn);
                target = tmpTarget;
                if (tmpProperty != null)
                {
                    //если ссылка на другой persistent - переходим к нему
                    if (tmpProperty.PropertyType != null)
                    {
                        tmpDataType = tmpProperty.PropertyType;
                        tmpTarget = tmpProperty.GetValue(tmpTarget, null);
                    }
                }
            }

            if (target != null && tmpProperty != null)
                tmpProperty.SetValue(target, value, null);
        }

        public static object ProcessNode(Session session, XPathNavigator item, Stack<Object> stack, Dictionary<string, object> variables, bool creation)
        {
            object dataObject = null;
            if (item == null) return null;

            switch (item.Name)
            {
                case "SectionReference":
                    ProcessNode(session, item.SelectSingleNode(string.Format("//Section[@id='{0}']", item.Value)), stack, variables, creation);
                    return null;

                case "Section":
                    LogManager.GetCurrentClassLogger().Info($"Обработка секции [{item.GetAttribute("name", "")}]");
                    break;

                case "Include":
                    LogManager.GetCurrentClassLogger().Info($"Обработка включения [{item.GetAttribute("filename", "")}]");

                    String fileName = item.GetAttribute("filename", "");
                    if (fileName.StartsWith("{BaseDirectory}"))
                    {
                        fileName = fileName.Replace("{BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
                    }

                    Trace.TraceInformation("Загрузка файла " + fileName);

                    XPathDocument source = new XPathDocument(fileName);

                    var navigator = source.CreateNavigator();
                    navigator.MoveToChild("DBInitPackage", "");
                    foreach (XPathNavigator node in navigator.SelectChildren(XPathNodeType.Element))
                    {
                        ProcessNode(session, node, stack, variables, creation);
                    }

                    break;

                case "Exec":
                    if (stack.Count == 0)
                        throw new Exception("Stack empty");

                    Object tmp = stack.Peek();

                    foreach (String memberName in item.GetAttribute("method", "").Split('.'))
                    {
                        MemberInfo member = tmp.GetType().GetMember(memberName)[0];
                        if (member == null)
                            throw new Exception("Member not found " + item.GetAttribute("method", ""));

                        if (member.MemberType == MemberTypes.Property)
                        {
                            PropertyInfo pi = tmp.GetType().GetProperty(member.Name);
                            tmp = pi.GetValue(tmp, null);
                        }

                        //execution
                        if (member.MemberType == MemberTypes.Method)
                        {
                            MethodInfo mi = tmp.GetType().GetMethod(member.Name);
                            ArrayList prm = new ArrayList();

                            int idx = 0;
                            foreach (XPathNavigator parameter in item.Select("Param/@val"))
                            {
                                ParameterInfo prmInfo = mi.GetParameters()[idx++];


                                if (parameter.Value.StartsWith("@"))
                                {
                                    //поиск по критерию
                                    string innerCriteria = parameter.Value.Substring(1, parameter.Value.Length - 1);
                                    var prmVal = session.FindObject(prmInfo.ParameterType, CriteriaOperator.Parse(innerCriteria));
                                    prm.Add(prmVal);

                                    //if (variables.ContainsKey(parameter.Value.Substring(1, parameter.Value.Length - 1)))
                                    //{
                                    //    prm.Add(variables[parameter.Value.Substring(1, parameter.Value.Length - 1)]);
                                    //}
                                    //else
                                    //{
                                    //    throw new Exception("Variable not found: " + parameter.Value);
                                    //}
                                }
                                else if (parameter.Value.StartsWith("#"))
                                {
                                    //try
                                    //{
                                    //    Object target = database.FindObjectByCriteria(typeof(EnumerationItem), "SysID = @0", new object[] { new Guid((String)StatickFieldRetriver(parameter.Value.Substring(1, parameter.Value.Length - 1))) });
                                    //    prm.Add(target);
                                    //}
                                    //catch
                                    //{
                                    //    throw new Exception("Referenced object not found: " + parameter.Value);
                                    //}
                                }
                                else
                                {
                                    String prmValue = parameter.Value;
                                    if (prmValue.StartsWith("{BaseDirectory}"))
                                    {
                                        prmValue = prmValue.Replace("{BaseDirectory}", AppDomain.CurrentDomain.BaseDirectory);
                                    }

                                    prm.Add(Convert.ChangeType(prmValue, prmInfo.ParameterType));
                                }
                            }

                            mi.Invoke(tmp, prm.ToArray());
                            return null;
                        }
                    }

                    break;

                case "Item":
                    XPathNavigator criteria = item.SelectSingleNode("(ancestor-or-self::*/@Criteria)[last()]");
                    XPathNavigator type = item.SelectSingleNode("(ancestor-or-self::*/@type)[last()]");
                    Type dataType = type != null ? GetObjectTypeByName(type.Value) : null;
                    if (dataType == null)
                    {
                        //throw new Exception("Type not found: " + type.Value);
                        Trace.TraceInformation($"Не найден тип данных {type.Value}");
                        break;
                    }

                    Dictionary<String, Object> valueMap = new Dictionary<String, Object>();
                    var list = from PropertyInfo pi in dataType.GetProperties() from XPathNavigator attribute in item.Select("@*") where pi.Name == attribute.Name || attribute.Name.StartsWith(pi.Name + ".") && pi.Name != "id" select new { pi, attribute };
                    foreach (var dataItem in list)
                    {
                        Type tmpDataType = dataType;

                        PropertyInfo tmpProperty = null;
                        if (dataItem.attribute.Name.Contains("."))
                        {
                            foreach (String pn in dataItem.attribute.Name.Split('.'))
                            {
                                tmpProperty = tmpDataType.GetProperty(pn);
                                if (tmpProperty != null)
                                    tmpDataType = tmpProperty.PropertyType;
                            }
                        }
                        else
                        {
                            tmpProperty = tmpDataType.GetProperty(dataItem.attribute.Name);
                        }

                        //обращение к переменной
                        if (dataItem.attribute.Value.StartsWith("?"))
                        {
                            if (variables.ContainsKey(dataItem.attribute.Value))
                            {
                                valueMap.Add(dataItem.attribute.Name, variables[dataItem.attribute.Value]);
                            }
                            else
                                throw new Exception("Variable not found: " + dataItem.attribute.Value);
                        }
                        else if (tmpProperty.Name == "SysID")
                        {
                            //восстанавливаем значение GUID
                            if (dataItem.attribute.Value == "NewGuid")
                            {
                                valueMap.Add(dataItem.attribute.Name, Guid.NewGuid());
                            }
                            else
                            {
                                valueMap.Add(dataItem.attribute.Name, new Guid(Convert.ToString(StatickFieldRetriver(dataItem.attribute.Value))));
                            }
                        }
                        else if (tmpProperty.PropertyType.IsEnum)
                        {
                            //восстанавливаем значение перечисления
                            valueMap.Add(dataItem.attribute.Name, Enum.Parse(tmpProperty.PropertyType, dataItem.attribute.Value));
                        }
                        else if (tmpProperty.PropertyType.GetInterfaces().FirstOrDefault(tmpIface => tmpIface == typeof(IDatabaseObject)) != null)
                        {
                            if (dataItem.attribute.Value == "null")
                                valueMap.Add(dataItem.attribute.Name, null);
                            else
                                //зарезервированные значения "Parent"
                                if (dataItem.attribute.Value == "Parent")
                            {
                                if (stack.Count == 0)
                                    throw new Exception("Стек пуст");

                                valueMap.Add(dataItem.attribute.Name, stack.Peek());
                            }
                            else
                            {
                                if (dataItem.attribute.Value == "")
                                    throw new Exception($"Значение атрибута {dataItem.pi.Name} не заполнено");

                                if (dataItem.attribute.Value.StartsWith("@"))
                                {
                                    //поиск по критерию
                                    string innerCriteria = dataItem.attribute.Value.Substring(1, dataItem.attribute.Value.Length - 1);
                                    try
                                    {
                                        valueMap.Add(dataItem.attribute.Name, session.FindObject(tmpProperty.PropertyType, CriteriaOperator.Parse(innerCriteria)));
                                    }
                                    catch
                                    {
                                        throw new Exception($"Не найден объект по критерию [{innerCriteria}]");
                                    }
                                }
                                else if (variables.ContainsKey(dataItem.attribute.Value))
                                {
                                    //поиск по имени
                                    valueMap.Add(dataItem.attribute.Name, variables[dataItem.attribute.Value]);
                                }
                                else
                                {
                                    try
                                    {
                                        //Object target = session.FindObjectByCriteria(tmpProperty.PropertyType, "SysID = @0", new object[] { new Guid((String)StatickFieldRetriver(dataItem.attribute.Value)) });
                                        //valueMap.Add(dataItem.attribute.Name, target);
                                    }
                                    catch
                                    {
                                        throw new Exception("Не найдена ссылка на объект: " + dataItem.attribute.Value);
                                    }
                                }
                            }
                        }
                        else
                        {
                            //восстанавливаем значение
                            if (tmpProperty.PropertyType == typeof(decimal))
                            {
                                string v = dataItem.attribute.Value;
                                string sep = System.Threading.Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
                                v = v.Replace(",", sep);
                                v = v.Replace(".", sep);
                                valueMap.Add(dataItem.attribute.Name, Convert.ChangeType(v, dataItem.pi.PropertyType));
                            }
                            else if (tmpProperty.PropertyType == typeof(double))
                            {
                                string v = dataItem.attribute.Value;
                                string sep = System.Threading.Thread.CurrentThread.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
                                v = v.Replace(",", sep);
                                v = v.Replace(".", sep);
                                valueMap.Add(dataItem.attribute.Name, Double.Parse(v, System.Globalization.CultureInfo.InvariantCulture));
                            }
                            else if (tmpProperty.PropertyType == typeof(Guid))
                                valueMap.Add(dataItem.attribute.Name, new Guid(dataItem.attribute.Value));
                            else if (tmpProperty.PropertyType == typeof(String))
                                valueMap.Add(dataItem.attribute.Name, WebUtility.HtmlDecode(dataItem.attribute.Value));
                            else
                                valueMap.Add(dataItem.attribute.Name, Convert.ChangeType(dataItem.attribute.Value, tmpProperty.PropertyType));
                        }
                    }

                    //поиск объекта по критерию
                    if (criteria != null)
                    {
                        Regex template = new Regex(@"(?<name>(?<pname>\w+)(!|\.)*\w*)\s*=\s*(?<value>(\?|\w+))");
                        List<Object> queryData = new List<Object>();
                        string expression = criteria.Value;
                        foreach (Match queryItem in template.Matches(criteria.Value))
                        {
                            if (queryItem.Groups["name"].Value.Contains("!Key"))
                            {
                                Object propertyValue = valueMap[queryItem.Groups["pname"].Value];
                                if (propertyValue != null)
                                {
                                    try
                                    {
                                        propertyValue = propertyValue.GetType().GetProperty("Id").GetValue(propertyValue, null);
                                    }
                                    catch
                                    {
                                        throw new Exception(String.Format("Error processing criteria. Taking property [Id] from {0}. NodeXml: {1}", queryItem.Groups["pname"].Value, item.OuterXml));
                                    }
                                }

                                expression = new Regex(queryItem.Groups["name"].Value).Replace(expression, queryItem.Groups["pname"].Value + ".Id", 1);
                                queryData.Add(propertyValue);
                            }
                            else if (queryItem.Groups["value"].Value == "?")
                                queryData.Add(valueMap[queryItem.Groups["name"].Value]);
                            else
                                queryData.Add(queryItem.Groups["value"].Value);

                            //expression = new Regex(@"\?").Replace(expression, "@" + count++, 1);
                        }

                        CriteriaOperator co = CriteriaOperator.Parse(expression, queryData.ToArray());
                        dataObject = session.FindObject(dataType, co);
                    }

                    if (dataObject == null)
                    {
                        dataObject = Activator.CreateInstance(dataType, new[] { session });
                    }

                    //заполняем свойства объекта
                    foreach (KeyValuePair<string, object> dataItem in valueMap)
                    {
                        try
                        {
                            SetMemberInfo(dataObject, dataItem.Key, dataItem.Value);
                        }
                        catch
                        {
                            throw new Exception("Set value error. Property: " + dataItem.Key);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(item.GetAttribute("id", "")) ? !variables.ContainsKey(item.GetAttribute("id", "")) : false)
                    {
                        variables.Add(item.GetAttribute("id", ""), dataObject);
                    }

                    stack.Push(dataObject);

                    if (!string.IsNullOrWhiteSpace(item.GetAttribute("MasterSave", "")))
                    {
                        try
                        {
                            var behaviors = DataObjectBehaviorAttribute.GetBhvInstancies(dataObject.GetType());

                            foreach (var bhv in behaviors)
                            {
                                try
                                {
                                    bhv.OnSaving(dataObject as IDatabaseObject);
                                }
                                catch (Exception ex)
                                {
                                    LogManager.GetCurrentClassLogger().Error(ex, $"Ошибка выполнения расширений при записи объекта данных {ex.Message} {ex.GetType().FullName}");
                                    throw;
                                }
                            }

                            (dataObject as XPBaseObject).Save();

                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error(ex, $"Ошибка записи данных {ex.Message} {ex.GetType().FullName}");
                            throw;
                        }
                    }

                    break;

                default:
                    break;
            }

            foreach (XPathNavigator subNode in item.SelectChildren(XPathNodeType.Element))
            {
                ProcessNode(session, subNode, stack, variables, creation);
            }

            if (item.Name == "Item")
            {
                dataObject = stack.Pop();

                if (string.IsNullOrWhiteSpace(item.GetAttribute("MasterSave", "")))
                {
                    try
                    {
                        var behaviors = DataObjectBehaviorAttribute.GetBhvInstancies(dataObject.GetType());

                        foreach (var bhv in behaviors)
                        {
                            try
                            {
                                bhv.OnSaving(dataObject as IDatabaseObject);
                            }
                            catch (Exception ex)
                            {
                                LogManager.GetCurrentClassLogger().Error(ex, $"Ошибка выполнения расширений при записи объекта данных {ex.Message} {ex.GetType().FullName}");
                                throw;
                            }
                        }

                        (dataObject as XPBaseObject).Save();

                    }
                    catch (Exception ex)
                    {
                        LogManager.GetCurrentClassLogger().Error(ex, $"Ошибка записи данных {ex.Message} {ex.GetType().FullName}");
                        throw;
                    }
                }

            }

            return dataObject;
        }

        public static bool UpdateObjectsOnDatabase(Session session, string initFile, Dictionary<string, object> variables = null, Boolean creation = false)
        {
            if (!System.IO.File.Exists(initFile))
            {
                LogManager.GetCurrentClassLogger().Error($"Файл инициализации {initFile} не найден");
                return false; 
            }

            try
            {
                LogManager.GetCurrentClassLogger().Info($"Зарузка файла инициализации {initFile}");
                XPathDocument source = new XPathDocument(initFile);

                variables = variables ?? new Dictionary<String, Object>();
                Stack<Object> stack = new Stack<Object>();

                LogManager.GetCurrentClassLogger().Info("Инициализация объектов данных");

                var navigator = source.CreateNavigator();
                navigator.MoveToChild("DBInitPackage", "");
                foreach (XPathNavigator node in navigator.SelectChildren(XPathNodeType.Element))
                {
                    ProcessNode(session, node, stack, variables, creation);
                }

                LogManager.GetCurrentClassLogger().Info("Инициализация объектов данных завершена!");
            }
            //catch (DbEntityValidationException vex)
            //{
            //    Trace.TraceError("Ошибка проверки данных!");

            //    foreach (var tmp in session.GetValidationErrors())
            //    {
            //        Trace.TraceError(String.Format("Ошибка проверки данных {0} {1}",
            //            tmp.Entry,
            //            String.Join(", ", tmp.ValidationErrors.Select(err => String.Format("{0} {1}", err.PropertyName, err.ErrorMessage))
            //        )));
            //    }

            //    throw vex;
            //}
            catch (ReflectionTypeLoadException lex)
            {
                LogManager.GetCurrentClassLogger().Error(lex, $"В процессе инициализации базы данных произошла ошибка загрузки типов, {lex.Message}");

                throw;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, $"В процессе инициализации базы данных произошла ошибка, {ex.Message}");

                throw;
            }

            return true;
        }

        #endregion


    }
}
