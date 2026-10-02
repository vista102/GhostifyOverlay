using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
public static class LifecycleTests {
    private sealed class Instruction { public int Offset; public MemberInfo Member; }
    private static List<MethodBase> Calls(MethodInfo method) { return Instructions(method).Where(x => x.Member is MethodBase).Select(x => (MethodBase)x.Member).ToList(); }
    private static List<Instruction> Instructions(MethodInfo method) {
        Dictionary<short, OpCode> codes = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(x => x.FieldType == typeof(OpCode)).Select(x => (OpCode)x.GetValue(null)).ToDictionary(x => x.Value);
        byte[] bytes = method.GetMethodBody().GetILAsByteArray();
        List<Instruction> result = new List<Instruction>();
        for (int i = 0; i < bytes.Length;) {
            int offset = i;
            short code = bytes[i++] == 0xfe ? (short)(0xfe00 | bytes[i++]) : (short)bytes[i - 1];
            OpCode opcode = codes[code];
            MemberInfo member = null;
            int size;
            switch (opcode.OperandType) {
                case OperandType.InlineNone: size = 0; break;
                case OperandType.ShortInlineBrTarget: case OperandType.ShortInlineI: case OperandType.ShortInlineVar: size = 1; break;
                case OperandType.InlineVar: size = 2; break;
                case OperandType.InlineI8: case OperandType.InlineR: size = 8; break;
                case OperandType.InlineSwitch: size = 4 + 4 * BitConverter.ToInt32(bytes, i); break;
                default: size = 4; break;
            }
            if (opcode.OperandType == OperandType.InlineMethod || opcode.OperandType == OperandType.InlineField)
                member = method.Module.ResolveMember(BitConverter.ToInt32(bytes, i));
            result.Add(new Instruction { Offset = offset, Member = member });
            i += size;
        }
        return result;
    }
}
