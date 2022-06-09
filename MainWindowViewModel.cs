using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System.Windows.Input;
using System.Threading;
using System.Windows;
using System.Collections.ObjectModel;
using System.Net;
using System.Resources;

namespace FileManage
{
    public class MainWindowViewModel : ObservableObject
    {
        public const int DISKBLOCKQUANTITY = 500;
        public const int DISKBLOCKSIZE = 2;

        public MainWindowViewModel()
        {
            SelectedIndex = -1;
            FileName = "";
            FileSize = DISKBLOCKSIZE;
            CreateQuantity = 50;
            HaveCreatedFileQuantity = 0;
            FreeBlockQuantity = DISKBLOCKQUANTITY;

            Files = new();
            FileNames = new();
            DiskBlocks = new();
            DiskPanels = new();

            for (int i = 0; i < DISKBLOCKQUANTITY; i++)
            {
                DiskBlocks.Add(new(i));
            }
            DiskPanels.Add(new(0, DISKBLOCKQUANTITY));
        }

        int RandomCreateFlag = 0;

        public ObservableCollection<string> FileNames { get; set; }

        public ObservableCollection<DiskBlock> DiskBlocks { get; set; }

        public ObservableCollection<DiskPanel> DiskPanels { get; set; }

        public ObservableCollection<File> Files { get; set; }

        private int freeBlockQuantity;

        public int FreeBlockQuantity { get => freeBlockQuantity; set => SetProperty(ref freeBlockQuantity, value); }

        private float fileSize;

        public float FileSize { get => fileSize; set => SetProperty(ref fileSize, value); }

        private string fileName;

        public string FileName { get => fileName; set => SetProperty(ref fileName, value); }


        private int selectedIndex;

        public int SelectedIndex { get => selectedIndex; set => SetProperty(ref selectedIndex, value); }

        private int createQuantity;

        public int CreateQuantity { get => createQuantity; set => SetProperty(ref createQuantity, value); }

        private int haveCreatedBlockQuantity;

        public int HaveCreatedFileQuantity { get => haveCreatedBlockQuantity; set => SetProperty(ref haveCreatedBlockQuantity, value); }

        private RelayCommand viewFileStatus;
        public ICommand ViewFileStatus => viewFileStatus ??= new RelayCommand(PerformViewFileStatus);

        private void PerformViewFileStatus()
        {
            if (SelectedIndex < 0)
            {
                return;
            }

            string fileInfo = "文件名：" + Files[SelectedIndex].FileName
                + "\r\n文件大小：" + Files[SelectedIndex].FileSize + "K";
            string fat = "\r\n文件占用的所有磁盘块：\r\n";
            foreach (var fatItem in Files[SelectedIndex].FAT)
            {
                fat += fatItem.ToString() + "  ";
            }
            MessageBox.Show(fileInfo + fat);
        }

        private RelayCommand deleteFile;
        public ICommand DeleteFile => deleteFile ??= new RelayCommand(PerformDeleteFile);

        private void PerformDeleteFile()
        {
            if (SelectedIndex < 0)
            {
                return;
            }

            // 释放空闲盘区
            for (int i = 0; i < DiskPanels.Count; i++)
            {
                if (DiskPanels[i].Did == Files[SelectedIndex].FAT[0])
                {
                    // 设置盘区状态
                    DiskPanels[i].FileName = null;
                    DiskPanels[i].Free = true;
                    int did = DiskPanels[i].NextDid;
                    int lastIndex = i;
                    while (did != -2)
                    {
                        for (int j = i; j < DiskPanels.Count; j++)
                        {
                            if (DiskPanels[j].Did == did)
                            {
                                DiskPanels[j].FileName = null;
                                DiskPanels[j].Free = true;
                                did = DiskPanels[j].NextDid;
                                lastIndex = j;
                                break;
                            }
                        }
                    }

                    // 链接前面的空闲盘区
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (DiskPanels[j].Free)
                        {
                            DiskPanels[j].NextDid = DiskPanels[i].Did;
                            break;
                        }
                    }
                    // 链接后面的空闲盘区
                    for (int j = lastIndex + 1; j < DiskPanels.Count; j++)
                    {
                        if (DiskPanels[j].Free)
                        {
                            DiskPanels[lastIndex].NextDid = DiskPanels[j].Did;
                            break;
                        }
                    }
                    // 这块就是最后一块空闲区
                    if (DiskPanels[lastIndex].NextDid == -2)
                    {
                        DiskPanels[lastIndex].NextDid = -1;
                    }

                    //for (int j = 0; j < DiskPanels.Count - 1; j++)
                    //{
                    //    if (DiskPanels[j].Free)
                    //    {
                    //        for (int k = j; k < DiskPanels.Count; k++)
                    //        {
                    //            if (DiskPanels[k].Free)
                    //            {
                    //                if(DiskPanels[j].Did + DiskPanels[j].DiskBlockQuantity == DiskPanels[k].Did)
                    //                {
                    //                    DiskPanels[j].NextDid = DiskPanels[k].NextDid;
                    //                    DiskPanels[j].DiskBlockQuantity += DiskPanels[k].DiskBlockQuantity;
                    //                    DiskPanels.RemoveAt(k);
                    //                }
                    //                break;
                    //            }
                    //        }
                    //    }
                    //}

                    break;
                }
            }

            // 清理磁盘块信息 实际上不需要 显示用
            foreach (var index in Files[SelectedIndex].FAT)
            {
                DiskBlocks[index].Free = true;
                DiskBlocks[index].FileName = null;
            }

            // 更新空闲磁盘块数
            FreeBlockQuantity += (int)Math.Ceiling(Files[SelectedIndex].FileSize / 2);

            int saveSelectedIndex = SelectedIndex;

            // 删除文件记录
            Files.RemoveAt(SelectedIndex);
            FileNames.RemoveAt(SelectedIndex);

            SelectedIndex = saveSelectedIndex;
            if (SelectedIndex == Files.Count)
            {
                SelectedIndex--;
            }

        }

        private RelayCommand deleteOddFile;
        public ICommand DeleteOddFile => deleteOddFile ??= new RelayCommand(PerformDeleteOddFile);

        private void PerformDeleteOddFile()
        {
            int saveSelectedIndex = SelectedIndex;

            for (int i = 1; i < 50; i += 2)
            {
                for (int j = 0; j < Files.Count; j++)
                {
                    if (Files[j].FileName == i.ToString() + ".txt")
                    {
                        SelectedIndex = j;
                        if (SelectedIndex <= saveSelectedIndex)
                        {
                            saveSelectedIndex--;
                        }
                        PerformDeleteFile();
                        break;
                    }
                }
            }

            SelectedIndex = saveSelectedIndex;

            if (SelectedIndex >= Files.Count)
            {
                SelectedIndex = Files.Count - 1;
            }
        }

        private RelayCommand randomCreateFile;
        public ICommand RandomCreateFile => randomCreateFile ??= new RelayCommand(PerformRandomCreateFile);

        private void PerformRandomCreateFile()
        {
            // 保存当前输入框
            string saveFileName = FileName;
            float saveFileSize = FileSize;

            Random random = new();
            int randomRangeLeft = 2;
            int randomRangeRight = 10;

            RandomCreateFlag = 1;

            for (int i = HaveCreatedFileQuantity; i < CreateQuantity + HaveCreatedFileQuantity; i++)
            {
                try
                {
                    FileName = (i + 1).ToString() + ".txt";
                    FileSize = random.Next(randomRangeLeft, randomRangeRight) + (float)random.Next(randomRangeLeft, randomRangeRight) / 10;
                    PerformCreateFile();
                }
                catch (Exception ex)
                {
                    if (ex.Message == "文件名重复")
                    {
                        i++;
                        HaveCreatedFileQuantity++;
                    }
                    else
                    {
                        MessageBox.Show("因磁盘空间不足，第" + (i - HaveCreatedFileQuantity + 1).ToString() +
                        "个文件" + (i + 1).ToString() + ".txt创建失败\r\n磁盘剩余块数：" + FreeBlockQuantity);

                        // 修改已创建数量
                        HaveCreatedFileQuantity -= CreateQuantity + HaveCreatedFileQuantity - i;
                        break;
                    }
                }
            }

            HaveCreatedFileQuantity += CreateQuantity;
            FileName = saveFileName;
            FileSize = saveFileSize;

            RandomCreateFlag = 0;
        }

        private RelayCommand viewFreeBlock;
        public ICommand ViewFreeBlock => viewFreeBlock ??= new RelayCommand(PerformViewFreeBlock);

        private void PerformViewFreeBlock()
        {
            string freeBlocks = "磁盘当前空闲区块共计" +
                FreeBlockQuantity + "块，块号列表如下：\r\n";
            for (int i = 0; i < DISKBLOCKQUANTITY; i++)
            {
                if (DiskBlocks[i].Free)
                {
                    freeBlocks += i + " ";
                }
            }
            MessageBox.Show(freeBlocks);
        }

        private RelayCommand createFile;
        public ICommand CreateFile => createFile ??= new RelayCommand(PerformCreateFile);

        private void PerformCreateFile()
        {
            try
            {
                if (FileName == "")
                {
                    throw new("文件名不能为空");
                }
                if (FileSize <= 0)
                {
                    throw new("请设置合适的文件大小");
                }
                if (FileSize / DISKBLOCKSIZE > FreeBlockQuantity)
                {
                    throw new("文件大小超出磁盘剩余空间");
                }
                foreach (var fileName in FileNames)
                {
                    if (fileName == FileName)
                    {
                        throw new("文件名重复");
                    }
                }

                AllocateDiskBlocks();

                // 第一个文件
                if (FileNames.Count == 1)
                {
                    SelectedIndex = 0;
                }

                if (RandomCreateFlag == 0)
                {
                    MessageBox.Show("文件创建成功");
                }
            }
            catch (Exception ex)
            {
                if (RandomCreateFlag == 0)
                {
                    MessageBox.Show(ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void AllocateDiskBlocks()
        {
            // 创建文件
            Files.Add(new(FileName, FileSize));
            FileNames.Add(FileName);

            int need = (int)Math.Ceiling(FileSize / DISKBLOCKSIZE);

            // 剩余空闲块更新
            FreeBlockQuantity -= need;

            for (int i = 0; need > 0 && i < DiskPanels.Count; i++)
            {
                while (DiskPanels[i].Free)
                {
                    // 恰好相等 直接分配
                    if (DiskPanels[i].DiskBlockQuantity == need)
                    {
                        DiskPanels[i].FileName = FileName;
                        DiskPanels[i].Free = false;
                        DiskPanels[i].NextDid = -2;

                        // 磁盘块记录文件信息
                        for (int j = DiskPanels[i].Did; j < DiskPanels[i].Did + need; j++)
                        {
                            DiskBlocks[j].Free = false;
                            DiskBlocks[j].FileName = FileName;

                            // 记录FAT表
                            Files[^1].FAT.Add(j);
                        }

                        need = 0;
                        break;
                    }
                    // 盘区大于文件所需
                    else if (DiskPanels[i].DiskBlockQuantity > need)
                    {
                        // 初始化后面的盘区
                        DiskPanels.Insert(i + 1, new(DiskPanels[i].Did + need, DiskPanels[i].DiskBlockQuantity - need));
                        DiskPanels[i + 1].NextDid = DiskPanels[i].NextDid;
                        DiskPanels[i + 1].Free = true;

                        // 分配当前盘区
                        DiskPanels[i].FileName = FileName;
                        DiskPanels[i].DiskBlockQuantity = need;
                        DiskPanels[i].Free = false;
                        DiskPanels[i].NextDid = -2;

                        // 磁盘块记录文件信息
                        for (int j = DiskPanels[i].Did; j < DiskPanels[i + 1].Did; j++)
                        {
                            DiskBlocks[j].Free = false;
                            DiskBlocks[j].FileName = FileName;

                            // 记录FAT表
                            Files[^1].FAT.Add(j);
                        }

                        need = 0;
                        break;
                    }
                    // 盘区不足文件所需
                    else
                    {
                        // 分配当前盘区
                        DiskPanels[i].FileName = FileName;
                        DiskPanels[i].Free = false;

                        // 磁盘块记录文件信息
                        for (int j = DiskPanels[i].Did; j < DiskPanels[i].Did + DiskPanels[i].DiskBlockQuantity; j++)
                        {
                            DiskBlocks[j].Free = false;
                            DiskBlocks[j].FileName = FileName;

                            // 记录FAT表
                            Files[^1].FAT.Add(j);
                        }

                        need -= DiskPanels[i].DiskBlockQuantity;

                        int k = i + 1;
                        while (DiskPanels[i].NextDid != DiskPanels[k].Did)
                            k++;
                        i = k;
                    }
                }
            }
        }

        private RelayCommand deleteAllFile;
        public ICommand DeleteAllFile => deleteAllFile ??= new RelayCommand(PerformDeleteAllFile);

        private void PerformDeleteAllFile()
        {
            while (Files.Count>0)
            {
                SelectedIndex = 0;
                PerformDeleteFile();
            }
        }
    }

    public class File : ObservableObject
    {
        public File(string name, float size)
        {
            FileName = name;
            FileSize = size;
            FAT = new();
        }

        private string fileName;
        public string FileName
        {
            get => fileName; set => SetProperty(ref fileName, value);
        }

        private float fileSize;
        public float FileSize
        {
            get => fileSize; set => SetProperty(ref fileSize, value);
        }

        private ObservableCollection<int> fAT;
        public ObservableCollection<int> FAT
        {
            get => fAT; set => SetProperty(ref fAT, value);
        }
    }

    public class Log : ObservableObject
    {
        public Log(int times, int nowLocation, int nextLocation, int distance)
        {
            Logs = times.ToString() + ".      当前处于磁道    " +
                nowLocation + "    下一次调度的目标磁道    " + nextLocation +
                "    寻道长度为    " + distance;
        }

        private string logs;
        public string Logs
        {
            get => logs; set => SetProperty(ref logs, value);
        }
    }

    public class DiskBlock : ObservableObject
    {
        public DiskBlock(int did)
        {
            Did = did;
            Free = true;
        }

        private int did;
        public int Did { get => did; set => SetProperty(ref did, value); }

        private string fileName;
        public string? FileName { get => fileName; set => SetProperty(ref fileName, value); }

        private bool free;
        public bool Free { get => free; set => SetProperty(ref free, value); }
    }

    public class DiskPanel : DiskBlock
    {
        public DiskPanel(int did, int quantity) : base(did)
        {
            Free = true;
            NextDid = -1;
            DiskBlockQuantity = quantity;
        }

        private int diskBlockQuantity;
        public int DiskBlockQuantity { get => diskBlockQuantity; set => SetProperty(ref diskBlockQuantity, value); }

        private int nextDid;
        public int NextDid { get => nextDid; set => SetProperty(ref nextDid, value); }
    }
}
