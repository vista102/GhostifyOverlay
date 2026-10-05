param(
    [string]$GameDir='C:\Program Files (x86)\Steam\steamapps\common\A Dance of Fire and Ice',
    [Parameter(Mandatory=$true)][string]$TypeName,
    [Parameter(Mandatory=$true)][string]$MethodNames
)
$ErrorActionPreference='Stop'
$taskManaged=Join-Path $GameDir 'A Dance of Fire and Ice_Data\Managed'
foreach($taskDir in @($taskManaged,(Join-Path $taskManaged 'UnityModManager'))){
    Get-ChildItem -LiteralPath $taskDir -Filter *.dll | ForEach-Object {
        try { [Reflection.Assembly]::LoadFrom($_.FullName)|Out-Null } catch {}
    }
}
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
public static class ReadOnlyGameIl {
    public static string[] Describe(MethodBase method) {
        var output = new List<string>();
        var opcodes = new Dictionary<short, OpCode>();
        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public)) {
            if (field.FieldType == typeof(OpCode)) {
                var opcode = (OpCode)field.GetValue(null);
                opcodes[opcode.Value] = opcode;
            }
        }
        var body = method.GetMethodBody();
        if (body == null) return new[] { "No method body" };
        byte[] bytes = body.GetILAsByteArray();
        Type[] typeArgs = method.DeclaringType.IsGenericType ? method.DeclaringType.GetGenericArguments() : null;
        Type[] methodArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;
        for (int i = 0; i < bytes.Length;) {
            int offset = i;
            short code = bytes[i++];
            if (code == 0xfe) code = (short)(0xfe00 | bytes[i++]);
            OpCode opcode = opcodes[code];
            int size;
            string operand = "";
            switch (opcode.OperandType) {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: size = 1; operand = (i + 1 + (sbyte)bytes[i]).ToString("x4"); break;
                case OperandType.ShortInlineI: size = 1; operand = ((sbyte)bytes[i]).ToString(); break;
                case OperandType.ShortInlineVar: size = 1; operand = bytes[i].ToString(); break;
                case OperandType.InlineVar: size = 2; operand = BitConverter.ToUInt16(bytes, i).ToString(); break;
                case OperandType.InlineI8: size = 8; operand = BitConverter.ToInt64(bytes, i).ToString(); break;
                case OperandType.InlineR: size = 8; operand = BitConverter.ToDouble(bytes, i).ToString("R", System.Globalization.CultureInfo.InvariantCulture); break;
                case OperandType.ShortInlineR: size = 4; operand = BitConverter.ToSingle(bytes, i).ToString("R", System.Globalization.CultureInfo.InvariantCulture); break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); operand = "count=" + BitConverter.ToInt32(bytes, i); break;
                case OperandType.InlineBrTarget: size = 4; operand = (i + 4 + BitConverter.ToInt32(bytes, i)).ToString("x4"); break;
                default: size = 4; operand = BitConverter.ToInt32(bytes, i).ToString(); break;
            }
            try {
                if (opcode.OperandType == OperandType.InlineField || opcode.OperandType == OperandType.InlineMethod || opcode.OperandType == OperandType.InlineTok || opcode.OperandType == OperandType.InlineType) {
                    MemberInfo member = method.Module.ResolveMember(BitConverter.ToInt32(bytes, i), typeArgs, methodArgs);
                    operand = member.DeclaringType + "." + member;
                    var dataField = member as FieldInfo;
                    if (opcode.Name == "ldtoken" && dataField != null && (dataField.Attributes & FieldAttributes.HasFieldRVA) != 0 && dataField.DeclaringType.TypeInitializer == null) {
                        int length = dataField.FieldType.StructLayoutAttribute.Size;
                        if (length > 0 && length <= 256 && length % 4 == 0) {
                            var data = new byte[length];
                            // Framework helper copies embedded RVA bytes; it does not run game code.
                            System.Runtime.CompilerServices.RuntimeHelpers.InitializeArray(data, dataField.FieldHandle);
                            var values = new List<string>();
                            for (int j = 0; j < length; j += 4) values.Add(BitConverter.ToInt32(data, j).ToString());
                            operand += " [RVA int32: " + string.Join(",", values.ToArray()) + "]";
                        }
                    }
                } else if (opcode.OperandType == OperandType.InlineString) {
                    operand = method.Module.ResolveString(BitConverter.ToInt32(bytes, i));
                }
            } catch (Exception error) { operand = "unresolved token: " + error.GetType().Name; }
            output.Add(offset.ToString("x4") + " " + opcode.Name + " " + operand);
            i += size;
        }
        return output.ToArray();
    }
}
'@
$taskType=$null
foreach($taskAssembly in [AppDomain]::CurrentDomain.GetAssemblies()){
    $taskType=$taskAssembly.GetType($TypeName,$false)
    if($taskType){break}
}
if(!$taskType){throw "Type not found: $TypeName"}
$taskFlags=[Reflection.BindingFlags]'Public,NonPublic,Static,Instance,DeclaredOnly'
$taskRequestedNames=$MethodNames -split ','
$taskMethods=@($taskType.GetMethods($taskFlags))+@($taskType.GetConstructors($taskFlags))
foreach($taskMethod in $taskMethods|Where-Object {$_.Name -in $taskRequestedNames}){
    $taskMethod.ToString()
    [ReadOnlyGameIl]::Describe($taskMethod)
}
# Reads method bodies without invoking game methods, constructors or initializers.
