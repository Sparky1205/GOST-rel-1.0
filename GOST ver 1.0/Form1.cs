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


namespace GOST_ver_1._0
{
    public partial class Form1 : Form 
    {
        byte[,] SubstitutionBox = new byte[,] {
            {4, 10, 9, 2, 13, 8, 0, 14, 6, 11, 1, 12, 7, 15, 5, 3},
            {14, 11, 4, 12, 6, 13, 15, 10, 2, 3, 8, 1, 0, 7, 5, 9},
            {5, 8, 1, 13, 10, 3, 4, 2, 14, 15, 12, 7, 6, 0, 9, 11},
            {7, 13, 10, 1, 0, 8, 9, 15, 14, 4, 6, 12, 11, 2, 5, 3},
            {6, 12, 7, 1, 5, 15, 13, 8, 4, 10, 9, 14, 0, 3, 11, 2},
            {4, 11, 10, 0, 7, 2, 1, 13, 3, 6, 8, 5, 9, 12, 15, 14},
            {13, 11, 4, 1, 3, 15, 5, 9, 0, 10, 14, 7, 6, 8, 2, 12},
            {1, 15, 13, 0, 5, 7, 10, 4, 9, 2, 3, 14, 6, 11, 8, 12}
            };

        //int[] keyMap = { 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 4, 5, 6, 7, 0, 1, 2, 3, 4, 5, 6, 7, 7, 6, 5, 4, 3, 2, 1, 0 };
        string file;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {


            Form1 form = new Form1();
            
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.InitialDirectory = "D:\\";
            openFileDialog1.Filter = "Rar (*.rar)|*.rar";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                file = openFileDialog1.FileName;
               
            }
            

        }

        private byte[] EncodeBlock(byte[] block, uint[] keys)
        {
            // separate on 2 blocks.
            uint N1 = BitConverter.ToUInt32(block, 0);
            uint N2 = BitConverter.ToUInt32(block, 4);

            for (int i = 0; i < 32; i++)
            {
                int keyIndex = i < 24 ? (i % 8) : (7 - i % 8); // to 24th cycle : 0 to 7; after - 7 to 0;
                var s = (N1 + keys[keyIndex]) % uint.MaxValue; // (N1 + X[i]) mod 2^32
                s = Substitution(s); // substitute from box
                s = (s << 11) | (s >> 21);
                s = s ^ N2; // ( s + N2 ) mod 2
                //N2 = N1;
                //N1 = s;
                if (i < 31) // last cycle : N1 don't change; N2 = s;
                {
                    N2 = N1;
                    N1 = s;
                }
                else
                {
                    N2 = s;
                }
            }

            var output = new byte[8];
            var N1buff = BitConverter.GetBytes(N1);
            var N2buff = BitConverter.GetBytes(N2);

            for (int i = 0; i < 4; i++)
            {
                output[i] = N1buff[i];
                output[4 + i] = N2buff[i];
            }

            return output;
        }

        byte[] Encode(byte[] data, byte[] key, bool isParallel = false)

        {
            var subkeys = GenerateKeys(key);
            var result = new byte[data.Length];
            var block = new byte[8];

            if (isParallel)
            {
                Parallel.For(0, data.Length / 8, i =>
                {
                    Array.Copy(data, 8 * i, block, 0, 8);
                    Array.Copy(EncodeBlock(block, subkeys), 0, result, 8 * i, 8);
                });
            }
            else
            {
                for (int i = 0; i < data.Length / 8; i++) // N blocks 64bits length.
                {
                    Array.Copy(data, 8 * i, block, 0, 8);
                    Array.Copy(EncodeBlock(block, subkeys), 0, result, 8 * i, 8);
                }
            }
            return result;
        }

        protected uint[] GenerateKeys(byte[] key)
        {
            if (key.Length != 32)
            {
                throw new Exception("Wrong key.");
            }

            var subkeys = new uint[8];

            for (int i = 0; i < 8; i++)
            {
                subkeys[i] = BitConverter.ToUInt32(key, 4 * i);
            }

            return subkeys;
        }

        protected uint Substitution(uint value)
        {
            

            uint output = 0;

            for (int i = 0; i < 8; i++)
            {
                var temp = (byte)((value >> (4 * i)) & 0x0f);
                temp = SubstitutionBox[i,temp];
                output |= (UInt32)temp << (4 * i);
            }

            return output;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            byte[] bytes = File.ReadAllBytes(@file);



            byte[] res = Encode(bytes, Encoding.Default.GetBytes(keyTextBox.Text), isParallel.Checked);
            /*
                        FileStream fileStream = new FileStream(textBox2.Text, FileMode.Create);

                        for (int i = 0; i < res.Length; i++)
                        {
                            fileStream.WriteByte(res[i]);
                        }*/
            sav(res);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();

            saveFileDialog1.Filter = "Rar (.*rar)|*.rar";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    // Code to write the stream goes here.
                    textBox2.Text = saveFileDialog1.FileName;
                    
                    myStream.Close();
                }
            }
        }

        byte[] Decode(byte[] data, byte[] key, bool isParallel = false)
        {
            var subkeys = GenerateKeys(key);
            var result = new byte[data.Length];
            var block = new byte[8];

            if (isParallel)
            {
                Parallel.For(0, data.Length / 8, i =>
                {
                    Array.Copy(data, 8 * i, block, 0, 8);
                    Array.Copy(DecodeBlock(block, subkeys), 0, result, 8 * i, 8);
                });
            }
            else
            {
                for (int i = 0; i < data.Length / 8; i++) // N blocks 64bits length.
                {
                    Array.Copy(data, 8 * i, block, 0, 8);
                    Array.Copy(DecodeBlock(block, subkeys), 0, result, 8 * i, 8);
                }
            }
            return result;
        }

        private byte[] DecodeBlock(byte[] block, uint[] keys)
        {
            // separate on 2 blocks.
            uint N1 = BitConverter.ToUInt32(block, 0);
            uint N2 = BitConverter.ToUInt32(block, 4);

            for (int i = 0; i < 32; i++)
            {
                int keyIndex = i < 8 ? (i % 8) : (7 - i % 8); // to 24th cycle : 0 to 7; after - 7 to 0;
                var s = (N1 + keys[keyIndex]) % uint.MaxValue; // (N1 + X[i]) mod 2^32
                s = Substitution(s); // substitute from box
                s = (s << 11) | (s >> 21);
                s = s ^ N2;
                if (i < 31) // last cycle : N1 don't change; N2 = s;
                {
                    N2 = N1;
                    N1 = s;
                }
                else
                {
                    N2 = s;
                }
            }

            var output = new byte[8];
            var N1buff = BitConverter.GetBytes(N1);
            var N2buff = BitConverter.GetBytes(N2);

            for (int i = 0; i < 4; i++)
            {
                output[i] = N1buff[i];
                output[4 + i] = N2buff[i];
            }

            return output;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            byte[] bytes = File.ReadAllBytes(@file);



            byte[] res = Decode(bytes, Encoding.Default.GetBytes(keyTextBox.Text), isParallel.Checked);
            /*
                        FileStream fileStream = new FileStream(textBox2.Text, FileMode.Create);

                        for (int i = 0; i < res.Length; i++)
                        {
                            fileStream.WriteByte(res[i]);
                        }

                        MessageBox.Show("Готово!");*/
            sav(res);
        }

        void sav (byte[] res)
        {
            using (FileStream fstream = new FileStream(textBox2.Text, FileMode.OpenOrCreate))
            {
      
                fstream.Write(res, 0, res.Length);
                Console.WriteLine("Текст записан в файл");
            }
        }
    }
    }


