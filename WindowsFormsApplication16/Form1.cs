using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication16
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_DragOver(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var rawdump = (string[])e.Data.GetData(DataFormats.FileDrop);

            var rawreader = new BinaryReader(new MemoryStream(File.ReadAllBytes(rawdump[0])));

            while (rawreader.BaseStream.Position != rawreader.BaseStream.Length)
            {
                var rawopcode = rawreader.ReadInt32();

                var optype = translaterawopcode(rawopcode);

                var rawopcodehexstring = string.Format("0x{0:X8}", rawopcode);

                var debug = string.Format("{0}  {1}", rawopcode >> 26 & 0x3f, rawopcode & 0x3f);
                dataGridView1.Rows.Add(rawopcodehexstring, optype.asm, debug);
            }
            
        }

        string[] register = {"r0", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7", "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra" };

        struct translatedopcode
        {
            public string asm;
            public string sharp;
        }

        struct Rinstr
        {
            public string opcode;
            public string rs;
            public string rt;
            public string rd;
            public int shamt;
            public int sa;
            public string funct;
        }

        struct Iinstr
        {
            public string opcode;
            public string rs;
            public string rt;
            public int immediate;
            public int offset;
            public string _base;
        }

        struct Jinstr
        {
            public string opcode;
            public string address;
        }

        Rinstr getRinstr(int rawopcode)
        {
            var instr = new Rinstr();
            instr.shamt = rawopcode >> 6 & 0x1f;
            instr.sa = rawopcode >> 6 & 0x1f;
            instr.rd = register[rawopcode >> 11 & 0x1f];
            instr.rt = register[rawopcode >> 16 & 0x1f];
            instr.rs = register[rawopcode >> 21 & 0x1f];
            return instr;
        }

        Iinstr getIinstr(int rawopcode)
        {
            var instr = new Iinstr();
            instr.immediate = (Int16)(rawopcode & 0xffff);
            instr.offset = (Int16)(rawopcode & 0xffff);
            instr.rt = register[rawopcode >> 16 & 0x1f];
            instr.rs = register[rawopcode >> 21 & 0x1f];
            instr._base = register[rawopcode >> 21 & 0x1f];
            return instr;
        }

        translatedopcode translaterawopcode(int rawopcode)
        {
            var type = rawopcode >> 26;
            var optype = new translatedopcode();
            switch(type)
            {
                case 0: optype.asm = "SPECIAL";
                    var opcode = rawopcode & 0x3f;
                    switch (opcode)
                    {
                        case 0://sll
                            var instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x2}", "sll", instr.rd, instr.rt, instr.sa);
                            break;
                        case 2://srl
                            instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x2}", "srl", instr.rd, instr.rt, instr.sa);
                            break;
                        case 4://sllv
                            instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, {3}", "sllv", instr.rd, instr.rt, instr.rs);
                            break;
                        case 8://jr
                            instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}", "jr", instr.rs);
                            break;
                        case 33://addu
                            instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, {3}", "addu", instr.rd, instr.rs, instr.rt);
                            break;
                        case 37://or
                            instr = getRinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, {3}", "or", instr.rd, instr.rs, instr.rt);
                            break;
                    }
                    break;
                case 1: optype.asm = "REGIMM";

                    break;
                default: optype.asm = "STANDARD";
                    opcode = rawopcode >> 26 & 0x3f;
                    switch (opcode)
                    {
                        case 5://bne
                            var instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x4}", "bne", instr.rs, instr.rt, instr.offset);
                            break;
                        case 9://addiu
                            instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x4}", "addiu", instr.rt, instr.rs, instr.immediate);
                            break;
                        case 10://slti
                            instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x4}", "slti", instr.rt, instr.rs, instr.immediate);
                            break;
                        case 12://andi
                            instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, {2}, ${3:x4}", "andi", instr.rt, instr.rs, instr.immediate);
                            break;
                        case 36://lbu
                            instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, ${2:x4}({3})", "lbu", instr.rt, instr.offset, instr._base);
                            break;
                        case 40://sb
                            instr = getIinstr(rawopcode);
                            optype.asm = string.Format("{0,-6} {1}, ${2:x4}({3})", "sb", instr.rt, instr.offset, instr._base);
                            break;
                    }
                    break;
            }
            return optype;
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            var cell = dataGridView1.SelectedCells[0];
            if (cell.ColumnIndex == 0)
            {
                var opcode = Convert.ToInt32(cell.Value);
                MessageBox.Show(opcode.ToString());
            }
        }
    }
}
