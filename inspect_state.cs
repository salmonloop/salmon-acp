using System;
using System.Linq;
using SalmonEgg.Presentation.Core.Mvux.Chat;
using Uno.Extensions.Reactive;

var state = State.Value(new object(), () => ChatConnectionState.Empty);
var type = state.GetType();
Console.WriteLine(type.FullName);
foreach (var iface in type.GetInterfaces().OrderBy(t => t.FullName))
{
    Console.WriteLine($"IFACE {iface.FullName}");
}
foreach (var prop in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy).OrderBy(p => p.Name))
{
    Console.WriteLine($"PROP {prop.PropertyType.FullName} {prop.Name}");
}
foreach (var method in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.FlattenHierarchy).Where(m => !m.IsSpecialName).OrderBy(m => m.Name).ThenBy(m => m.GetParameters().Length))
{
    var args = string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name));
    Console.WriteLine($"METHOD {m.ReturnType.Name} {m.Name}({args})");
}
