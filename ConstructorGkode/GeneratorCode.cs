﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CNC_Controller.ConstructorGkode;
using CNC_Controller.primitiv;

namespace CNC_Controller
{
    public partial class GeneratorCode : Form
    {
        /// <summary>
        /// Ссылка на основную форму
        /// </summary>
        private readonly MainForm _mf;

        // Для упращения ввода новых данных, запоминается последняя введенная координата,
        // и при добавлении нового примитива эти координаты устанавливаются
        private double _lastX;       // координата в мм
        private double _lastY;       // координата в мм
        private double _lastZ;       // координата в мм

        /// <summary>
        /// Список примитивов
        /// </summary>
        private List<primitivNode> _listPrimitives = new List<primitivNode>();



        #region Элементы формы добавления данных

        private void GeneratorCode_Load(object sender, EventArgs e)
        {
            _listPrimitives.Clear();
            RefreshTree();
        }

        public GeneratorCode(MainForm _mf)
        {
            this._mf = _mf;
            InitializeComponent();
        }

        private void btNewData_Click(object sender, EventArgs e)
        {
            _listPrimitives.Clear();
            RefreshTree();
        }

        private void treeDataConstructor_AfterSelect(object sender, TreeViewEventArgs e)
        {
            // что-бы не менялся значек
            if (treeDataConstructor.SelectedNode != null)
            {
                treeDataConstructor.SelectedImageIndex = treeDataConstructor.SelectedNode.ImageIndex;
            }
        }


        #endregion
 
        #region Добавление новых примитивов

        // Рекурсивный поиск внутри примитивов, других примитивов
        public primitivNode findNodeWithGUID(string _guid, primitivNode _node = null)
        {
            primitivNode result = null;

            primitivNode RootNode = _node;

            if (RootNode == null) RootNode = _listPrimitives[0];

            if (RootNode.GUID == _guid) return RootNode;

            foreach (primitivNode VARIABLE in RootNode.nodes)
            {
                if (VARIABLE.GUID == _guid) return VARIABLE;

                //так-же запустим поиск внутри подчиненных узлов
                primitivNode ppn = findNodeWithGUID(_guid, VARIABLE);

                if (ppn != null) return ppn;
            }
            return result;
        }

        // Добавление новой группы в "дерево данных"
        public void AddNewGroup()
        {
            TreeNode pNode = treeDataConstructor.SelectedNode;

            // попытаемся получить вышестоящий узел в дереве
            if (pNode == null) pNode = treeDataConstructor.Nodes[0];

            if (pNode == null) // всетаки неудалось.......
            {
                MessageBox.Show(@"DANGER!!! Ошибка указания родителя?!?!");
                return;
            }

            // найдем вышестоящий узел в ListPrimitives
            primitivNode pnfind = findNodeWithGUID(pNode.Name);

            if (pnfind == null)
            {
                MessageBox.Show(@"DANGER!!! Ошибка поиска родителя?!?!");
                return;
            }

            // Сразу проверим узел выше, т.к. каталог можно создавать в корне дерева, или внутри другого каталога, а создание каталога внутри других элементов нелогично
            if (!(pnfind.typeNode == primitivType.catalog || pnfind.typeNode == primitivType.cycler || pnfind.typeNode == primitivType.rotate))
            {
                MessageBox.Show(@"Создание каталога в нутри данного примитива невозможно!");
                return;
            }

            // вызовем диалог добавления группы
            frmCatalog fCatalog = new frmCatalog(_mf);
            fCatalog.deltaX.Value = (decimal)_lastX;
            fCatalog.deltaY.Value = (decimal)_lastY;
            fCatalog.deltaZ.Value = (decimal)_lastZ;

            DialogResult dlResult = fCatalog.ShowDialog();

            if (dlResult == DialogResult.OK)
            {
                _lastX = (double)fCatalog.deltaX.Value;
                _lastY = (double)fCatalog.deltaY.Value;
                _lastZ = (double)fCatalog.deltaZ.Value;

                primitivNode pn = new primitivNode(new primitivCatalog((double)fCatalog.deltaX.Value, (double)fCatalog.deltaY.Value, (double)fCatalog.deltaZ.Value, (double)fCatalog.deltaRotate.Value, fCatalog.textBoxName.Text));
                pnfind.nodes.Add(pn);
                RefreshTree();
            }
        }

        // Добавление новой точки в дерево
        public void AddNewPoint()
        {
            TreeNode pNode = treeDataConstructor.SelectedNode;

            // попытаемся получить вышестоящий узел в дереве
            if (pNode == null) pNode = treeDataConstructor.Nodes[0];

            if (pNode == null) // всетаки неудалось.......
            {
                MessageBox.Show(@"DANGER!!! Ошибка указания родителя?!?!");
                return;
            }

            // найдем вышестоящий узел в ListPrimitives
            primitivNode pnfind = findNodeWithGUID(pNode.Name);

            if (pnfind == null)
            {
                MessageBox.Show(@"DANGER!!! Ошибка поиска родителя?!?!");
                return;
            }

            // Сразу проверим узел выше, т.к. точку внутри точки создавать нелогично
            if (pnfind.typeNode == primitivType.point)
            {
                MessageBox.Show(@"Создание точки в нутри данного примитива невозможно!");
                return;
            }

            frmPoint fPoint = new frmPoint();
            fPoint.numPosX.Value = (decimal)_lastX;
            fPoint.numPosY.Value = (decimal)_lastY;
            fPoint.numPosZ.Value = (decimal)_lastZ;

            DialogResult dlResult = fPoint.ShowDialog();

            if (dlResult == DialogResult.OK)
            {
                _lastX = (double)fPoint.numPosX.Value;
                _lastY = (double)fPoint.numPosY.Value;
                _lastZ = (double)fPoint.numPosZ.Value;

                primitivNode pn = new primitivNode(new primitivPoint((double)fPoint.numPosX.Value, (double)fPoint.numPosY.Value, (double)fPoint.numPosZ.Value));
                pnfind.nodes.Add(pn);
                RefreshTree();
            }
        }


        public void AddNewCycle()
        {
            TreeNode pNode = treeDataConstructor.SelectedNode;

            // попытаемся получить вышестоящий узел в дереве
            if (pNode == null) pNode = treeDataConstructor.Nodes[0];

            if (pNode == null) // всетаки неудалось.......
            {
                MessageBox.Show(@"DANGER!!! Ошибка указания родителя?!?!");
                return;
            }

            // найдем вышестоящий узел в ListPrimitives
            primitivNode pnfind = findNodeWithGUID(pNode.Name);

            if (pnfind == null)
            {
                MessageBox.Show(@"DANGER!!! Ошибка поиска родителя?!?!");
                return;
            }

            // Сразу проверим узел выше, т.к. Циклёр можно создавать в корне дерева, или внутри другого каталога, а создание Циклёра внутри других элементов нелогично
            if (!(pnfind.typeNode == primitivType.catalog || pnfind.typeNode == primitivType.cycler || pnfind.typeNode == primitivType.rotate))
            {
                MessageBox.Show(@"Создание циклёра в нутри данного примитива невозможно!");
                return;
            }

            // вызовем диалог добавления циклёра
            frmCycler fCycler = new frmCycler(_mf);

            DialogResult dlResult = fCycler.ShowDialog();

            if (dlResult == DialogResult.OK)
            {
                primitivNode pn = new primitivNode(new primitivCycle((double)fCycler.numStart.Value, (double)fCycler.numStop.Value, (double)fCycler.numStep.Value, fCycler.cbX.Checked, fCycler.cbY.Checked, fCycler.cbZ.Checked, fCycler.textBoxName.Text));
                pnfind.nodes.Add(pn);
                RefreshTree();
            }

        }


        public void AddNewRotate()
        {
            TreeNode pNode = treeDataConstructor.SelectedNode;

            // попытаемся получить вышестоящий узел в дереве
            if (pNode == null) pNode = treeDataConstructor.Nodes[0];

            if (pNode == null) // всетаки неудалось.......
            {
                MessageBox.Show(@"DANGER!!! Ошибка указания родителя?!?!");
                return;
            }

            // найдем вышестоящий узел в ListPrimitives
            primitivNode pnfind = findNodeWithGUID(pNode.Name);

            if (pnfind == null)
            {
                MessageBox.Show(@"DANGER!!! Ошибка поиска родителя?!?!");
                return;
            }

            // Сразу проверим узел выше, т.к. Циклёр можно создавать в корне дерева, или внутри другого каталога, а создание Циклёра внутри других элементов нелогично
            if (!(pnfind.typeNode == primitivType.catalog || pnfind.typeNode == primitivType.cycler || pnfind.typeNode == primitivType.rotate))
            {
                MessageBox.Show(@"Создание циклёра в нутри данного примитива невозможно!");
                return;
            }

            // вызовем диалог добавления циклёра
            frmRotate frotate = new frmRotate(_mf);

            DialogResult dlResult = frotate.ShowDialog();

            if (dlResult == DialogResult.OK)
            {
                primitivNode pn = new primitivNode(new primitivRotate((double)frotate.centerX.Value, (double)frotate.centerY.Value, (double)frotate.centerZ.Value, (double)frotate.rotateStartAngle.Value, (double)frotate.rotateStopAngle.Value, (double)frotate.rotateStepAngle.Value, (double)frotate.rotateRadius.Value, (double)frotate.deltaStepRadius.Value, (double)frotate.RotateRotates.Value, frotate.textBoxName.Text));
                pnfind.nodes.Add(pn);
                RefreshTree();
            }
        }

        #endregion


        private void OpenFormDialog()
        {
            if (treeDataConstructor.SelectedNode == null) return;

            // Необходимость перезаполнения дерева обновленными данными
            bool NeedRefreshTree = false;

            //получим примитив по гуиду
            primitivNode pfind = findNodeWithGUID(treeDataConstructor.SelectedNode.Name);
            //определим его тип, и откроем необходимый диалог

            if (pfind.typeNode == primitivType.catalog)
            {
                // вызовем диалог добавления группы
                frmCatalog fCatalog = new frmCatalog(_mf);
                fCatalog.deltaX.Value = (decimal)pfind.catalog.deltaX;
                fCatalog.deltaY.Value = (decimal)pfind.catalog.deltaY;
                fCatalog.deltaZ.Value = (decimal)pfind.catalog.deltaZ;
                fCatalog.textBoxName.Text = pfind.catalog.Name;
                fCatalog.deltaRotate.Value = (decimal)pfind.catalog.deltaRotate;

                DialogResult dlResult = fCatalog.ShowDialog();

                if (dlResult == DialogResult.OK)
                {
                    pfind.catalog.deltaX = (double)fCatalog.deltaX.Value;
                    pfind.catalog.deltaY = (double)fCatalog.deltaY.Value;
                    pfind.catalog.deltaZ = (double)fCatalog.deltaZ.Value;
                    pfind.catalog.Name = fCatalog.textBoxName.Text;
                    pfind.catalog.deltaRotate = (double)fCatalog.deltaRotate.Value;

                    NeedRefreshTree = true;
                }
            }

            if (pfind.typeNode == primitivType.cycler)
            {
                // вызовем диалог добавления группы
                frmCycler fCycler = new frmCycler(_mf);

                fCycler.numStart.Value = (decimal)pfind.cycler.cStart;
                fCycler.numStop.Value = (decimal)pfind.cycler.cStop;
                fCycler.numStep.Value = (decimal)pfind.cycler.cStep;
                fCycler.textBoxName.Text = pfind.cycler.Name;
                fCycler.cbX.Checked = pfind.cycler.AllowDeltaX;
                fCycler.cbY.Checked = pfind.cycler.AllowDeltaY;
                fCycler.cbZ.Checked = pfind.cycler.AllowDeltaZ;

                DialogResult dlResult = fCycler.ShowDialog();

                if (dlResult == DialogResult.OK)
                {
                    pfind.cycler.cStart = (double)fCycler.numStart.Value;
                    pfind.cycler.cStop = (double)fCycler.numStop.Value;
                    pfind.cycler.cStep = (double)fCycler.numStep.Value;

                    pfind.cycler.Name = fCycler.textBoxName.Text;
                    pfind.cycler.AllowDeltaX = fCycler.cbX.Checked;
                    pfind.cycler.AllowDeltaY = fCycler.cbY.Checked;
                    pfind.cycler.AllowDeltaZ = fCycler.cbZ.Checked;

                    NeedRefreshTree = true;
                }
            }


            if (pfind.typeNode == primitivType.rotate)
            {
                // вызовем диалог добавления группы
                frmRotate frotate = new frmRotate(_mf);

                frotate.centerX.Value = (decimal)pfind.rotate.X;
                frotate.centerY.Value = (decimal)pfind.rotate.Y;
                frotate.centerZ.Value = (decimal)pfind.rotate.Z;

                frotate.rotateRadius.Value = (decimal)pfind.rotate.radius;
                frotate.rotateStartAngle.Value = (decimal)pfind.rotate.angleStart;
                frotate.rotateStopAngle.Value = (decimal)pfind.rotate.angleStop;
                frotate.rotateStepAngle.Value = (decimal)pfind.rotate.angleStep;


                frotate.deltaStepRadius.Value = (decimal)pfind.rotate.deltaStepRadius;
                frotate.RotateRotates.Value = (decimal)pfind.rotate.RotateRotates;

                frotate.textBoxName.Text = pfind.rotate.Name;


                DialogResult dlResult = frotate.ShowDialog();

                if (dlResult == DialogResult.OK)
                {
                    pfind.rotate.Name = frotate.textBoxName.Text;

                    pfind.rotate.X = (double)frotate.centerX.Value;
                    pfind.rotate.Y=(double)frotate.centerY.Value;
                    pfind.rotate.Z=(double)frotate.centerZ.Value;

                    pfind.rotate.radius=(double)frotate.rotateRadius.Value;
                    pfind.rotate.angleStart=(double)frotate.rotateStartAngle.Value;
                    pfind.rotate.angleStop=(double)frotate.rotateStopAngle.Value;
                    pfind.rotate.angleStep=(double)frotate.rotateStepAngle.Value;

                    pfind.rotate.deltaStepRadius = (double)frotate.deltaStepRadius.Value;
                    pfind.rotate.RotateRotates = (double)frotate.RotateRotates.Value;

                    NeedRefreshTree = true;
                }
            }



            if (pfind.typeNode == primitivType.point)
            {
                // вызовем диалог добавления группы
                frmPoint fPoint = new frmPoint();
                fPoint.numPosX.Value = (decimal)pfind.point.X;
                fPoint.numPosY.Value = (decimal)pfind.point.Y;
                fPoint.numPosZ.Value = (decimal)pfind.point.Z;

                DialogResult dlResult = fPoint.ShowDialog();

                if (dlResult == DialogResult.OK)
                {
                    pfind.point.X = (double)fPoint.numPosX.Value;
                    pfind.point.Y = (double)fPoint.numPosY.Value;
                    pfind.point.Z = (double)fPoint.numPosZ.Value;
                    NeedRefreshTree = true;
                }
            }

            if (NeedRefreshTree) RefreshTree();
        }


        // Открытие диалогов у существующих примитивов
        private void treeDataConstructor_DoubleClick(object sender, EventArgs e)
        {
            OpenFormDialog();
        }

        // рекурсивная функция для перерисовки дерева
        private void DrawPrimitivInTree(primitivNode _primitivNode, TreeNode _rootNode = null)
        {
            TreeNode trNode = null;

            if (_rootNode == null)
            {
                trNode = treeDataConstructor.Nodes.Add("");
            }
            else
            {
                trNode = _rootNode.Nodes.Add("");
            }

            if (_primitivNode.typeNode == primitivType.catalog)
            {
                trNode.ImageIndex = 1;
                trNode.Name = _primitivNode.GUID;
                trNode.Text = _primitivNode.catalog.Name;
            }

            if (_primitivNode.typeNode == primitivType.cycler)
            {
                trNode.ImageIndex = 4;
                trNode.Name = _primitivNode.GUID;

                trNode.Text = _primitivNode.cycler.Name + "(с: " + _primitivNode.cycler.cStart + " по: " + _primitivNode.cycler.cStop + " шаг:" + +_primitivNode.cycler.cStep + ")";
            }

            if (_primitivNode.typeNode == primitivType.rotate)
            {
                trNode.ImageIndex = 6;
                trNode.Name = _primitivNode.GUID;

                trNode.Text = _primitivNode.rotate.Name;// + "(с: " + _primitivNode.cycler.cStart + " по: " + _primitivNode.cycler.cStop + " шаг:" + +_primitivNode.cycler.cStep + ")";
            }



            if (_primitivNode.typeNode == primitivType.point)
            {
                trNode.ImageIndex = 2;
                trNode.Name = _primitivNode.GUID;
                trNode.Text = "Точка: (" + _primitivNode.point.X + ", " + _primitivNode.point.Y + ", "  + _primitivNode.point.Z + ")";
            }

            if (_primitivNode.nodes.Count == 0) return;

            foreach (primitivNode VARIABLE in _primitivNode.nodes)
                DrawPrimitivInTree(VARIABLE, trNode);
        }
        
        // перерисовка данных
        private void RefreshTree()
        {
            //элемент на котом нужно будет спозиционироваться
            string GUIDselectedNode = "";

            if (treeDataConstructor.SelectedNode != null) GUIDselectedNode = treeDataConstructor.SelectedNode.Name;

            treeDataConstructor.Nodes.Clear();

            //Если нет данных
            if (_listPrimitives.Count == 0)
            {
                primitivNode pp = new primitivNode(new primitivCatalog(0, 0, 0, 0, "Точка старта"));
                _listPrimitives.Add(pp);
                GUIDselectedNode = pp.GUID;
            }

            treeDataConstructor.BeginUpdate();
            treeDataConstructor.Nodes.Clear();

            DrawPrimitivInTree(_listPrimitives[0]);

            treeDataConstructor.EndUpdate();
            treeDataConstructor.ExpandAll();

            // и G-код сразу сгенерируем
            CREATE_GKOD();


            // установим активность на узле с GUIDselectedNode
            TreeNode[] trArray = treeDataConstructor.Nodes.Find(GUIDselectedNode, true);

            if (trArray.Length != 0) treeDataConstructor.SelectedNode = trArray[0];


        }


        // Получение G-кода из данных конструктора
        private void ParsePrimitivesToGkode(ref string _strCode, primitivNode _node, double deltaX = 0, double deltaY = 0, double deltaZ = 0, double deltaRotate = 0)
        {
            if (_node.typeNode == primitivType.point)
            {
                double rotatedX = _node.point.X * Math.Cos(deltaRotate * (Math.PI / 180)) - _node.point.Y * Math.Sin(deltaRotate * (Math.PI / 180));
                double rotatedY = _node.point.X * Math.Sin(deltaRotate * (Math.PI / 180)) + _node.point.Y * Math.Cos(deltaRotate * (Math.PI / 180));

                double xpp = rotatedX + deltaX;
                double ypp = rotatedY + deltaY;
                double zpp = _node.point.Z + deltaZ;

                _strCode += "G1 X" + (Math.Round(xpp,3)) + " Y" + (Math.Round(ypp,3)) + " Z" + (zpp) + "\n";
                //_strCode += "G1 X" + (_node.point.X + deltaX) + " Y" + (_node.point.Y + deltaY) + " Z" + (_node.point.Z + deltaZ) + "\n";
                return;
            }


            if (_node.typeNode == primitivType.catalog)
            {
                double dX = deltaX;       // координата в мм
                double dY = deltaY;       // координата в мм
                double dZ = deltaZ;       // координата в мм
                double dR = deltaRotate;  // значение в градусах

                dX += _node.catalog.deltaX;
                dY += _node.catalog.deltaY;
                dZ += _node.catalog.deltaZ;
                dR += _node.catalog.deltaRotate;

                foreach (primitivNode VARIABLE in _node.nodes)
                {
                    ParsePrimitivesToGkode(ref _strCode,VARIABLE,dX,dY,dZ,dR);
                }
            }



            if (_node.typeNode == primitivType.cycler)
            {
                if (_node.cycler.cStart < _node.cycler.cStop)
                {
                    for (double i = _node.cycler.cStart; i < _node.cycler.cStop; i += _node.cycler.cStep)
                    {
                    
                        double dX = deltaX;       // координата в мм
                        double dY = deltaY;       // координата в мм
                        double dZ = deltaZ;       // координата в мм

                        if (_node.cycler.AllowDeltaX) dX += i;

                        if (_node.cycler.AllowDeltaY) dY += i;

                        if (_node.cycler.AllowDeltaZ) dZ += i;
                    
                        foreach (primitivNode VARIABLE in _node.nodes)
                        {
                            ParsePrimitivesToGkode(ref _strCode, VARIABLE, dX, dY, dZ);
                        }
                    }    
                }
                else
                {
                    for (double i = _node.cycler.cStart; i > _node.cycler.cStop; i -= _node.cycler.cStep)
                    {

                        double dX = deltaX;       // координата в мм
                        double dY = deltaY;       // координата в мм
                        double dZ = deltaZ;       // координата в мм

                        if (_node.cycler.AllowDeltaX) dX += i;

                        if (_node.cycler.AllowDeltaY) dY += i;

                        if (_node.cycler.AllowDeltaZ) dZ += i;

                        foreach (primitivNode VARIABLE in _node.nodes)
                        {
                            ParsePrimitivesToGkode(ref _strCode, VARIABLE, dX, dY, dZ);
                        }
                    }
                }
            }  
            
            if (_node.typeNode == primitivType.rotate)
                {
                    double dX = 0;       // координата в мм
                    double dY = 0;       // координата в мм
                    double dZ = deltaZ;  // координата в мм

                    double dSR = _node.rotate.deltaStepRadius; // дельта изменения радиуса с каждым шагом
                    double dRO = _node.rotate.RotateRotates;   // угол дополнительного вращения объекта
                    double nowdRO = 0;

                    double dRadius = _node.rotate.radius;

                    for (double angle = _node.rotate.angleStart; angle < _node.rotate.angleStop; angle += _node.rotate.angleStep)
                    {
                        double x1 = _node.rotate.X + dRadius * Math.Cos(angle * (Math.PI / 180));
                        double y1 = _node.rotate.Y + dRadius * Math.Sin(angle * (Math.PI / 180));


                        dX = x1+deltaX;
                        dY = y1+deltaY;
                        //dZ += _node.catalog.Z;

                        foreach (primitivNode VARIABLE in _node.nodes)
                        {
                            ParsePrimitivesToGkode(ref _strCode, VARIABLE, dX, dY, dZ, nowdRO);
                            nowdRO += dRO;

                        }

                        dRadius += dSR;

                    }
                }
        }

        private void CREATE_GKOD()
        {
            string code = "";

            if (_listPrimitives.Count == 0) return;

            ParsePrimitivesToGkode(ref code, _listPrimitives[0]);

            //пошлем сгенерированный код
            _mf.LoadDataFromText(Regex.Split(code, "\n"));
        }

        private void DeleteNode(string _GUID, primitivNode _primitiv)
        {
            foreach (primitivNode VARIABLE in _primitiv.nodes)
            {
                if (VARIABLE.GUID == _GUID)
                {
                    _primitiv.nodes.Remove(VARIABLE);
                    break; //дальше продолжать нельзя, т.к. сломали выборку
                }
                else DeleteNode(_GUID, VARIABLE);
            }
        }


        private void openDialogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFormDialog();
        }

        private void btSaveToFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = @"Данные конструктора (*.dat)|*.dat|Все файлы (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                BinaryFormatter binFormat = new BinaryFormatter();
                using (Stream fStream = new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    binFormat.Serialize(fStream, _listPrimitives[0]);
                }
            }
        }

        private void btLoadFromFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Данные конструктора (*.dat)|*.dat|Все файлы (*.*)|*.*";

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                BinaryFormatter binFormat = new BinaryFormatter();

                using (Stream fStream = File.OpenRead(openFileDialog1.FileName))
                {
                    primitivNode pNode = (primitivNode)binFormat.Deserialize(fStream);
                    _listPrimitives[0] = pNode;
                }
                RefreshTree();
            }
        }

        private void treeDataConstructor_BeforeCollapse(object sender, TreeViewCancelEventArgs e)
        {
            e.Cancel = true;
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddNewGroup();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            AddNewPoint();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            AddNewCycle();
        }

        /// <summary>
        /// Буфер для хранения промежуточных данных
        /// </summary>
        private primitivNode clipboardPrimitivNode = null;

        // контекст меню копировать
        private void ToolStripMenuCopyDATA_Click(object sender, EventArgs e)
        {
            if (treeDataConstructor.SelectedNode == null) return;

            clipboardPrimitivNode = findNodeWithGUID(treeDataConstructor.SelectedNode.Name);
        }
        // контекст меню вставить
        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeDataConstructor.SelectedNode == null) return;

            primitivNode tmp = findNodeWithGUID(treeDataConstructor.SelectedNode.Name);

            tmp.nodes.Add(clipboardPrimitivNode);

            clipboardPrimitivNode = null;

            RefreshTree();
        }
        // контекст меню вырезать
        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeDataConstructor.SelectedNode == null) return;

            clipboardPrimitivNode = findNodeWithGUID(treeDataConstructor.SelectedNode.Name);

            DeleteNode(treeDataConstructor.SelectedNode.Name, _listPrimitives[0]);

            RefreshTree();
        }
        // контекст меню удалить
        private void delPrimitivToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeDataConstructor.SelectedNode == null) return;

            if (MessageBox.Show("Удалить выделенный примитив?", "Удаление", MessageBoxButtons.OKCancel) != DialogResult.OK) return;

            DeleteNode(treeDataConstructor.SelectedNode.Name, _listPrimitives[0]);

            RefreshTree();
        }

        private void contextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            pasteToolStripMenuItem.Enabled = (clipboardPrimitivNode != null);
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            AddNewRotate();
        }

        private void SaveToFile_Click(object sender, EventArgs e)
        {
            if (_listPrimitives.Count == 0) return;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = @"G-код (*.txt)|*.txt|Все файлы (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string code = "";
                ParsePrimitivesToGkode(ref code, _listPrimitives[0]);

                StreamWriter SW = new StreamWriter(new FileStream(saveFileDialog.FileName, FileMode.Create, FileAccess.Write));
                SW.Write(code);
                SW.Close();
            }
        }
    }
}

/// <summary>
/// Типы примитивов
/// </summary>
[Serializable]
public enum primitivType
{
    catalog,
    point,
    cycler,
    rotate
}

/// <summary>
/// Класс описания группы элементов
/// </summary>
[Serializable]
public class primitivCatalog
{
    //Смещения елементов в нутри данной группы
    public double deltaX;       // координата в мм
    public double deltaY;       // координата в мм
    public double deltaZ;       // координата в мм

    //угол поворота
    public double deltaRotate;       // координата в мм
    
    // для представления
    public string Name;

    public primitivCatalog(double _deltaX, double _deltaY, double _deltaZ, double _deltaRotate, string _name = "Группа")
    {
        deltaX = _deltaX;
        deltaY = _deltaY;
        deltaZ = _deltaZ;
        deltaRotate = _deltaRotate;
        Name = _name;
    }
}

/// <summary>
/// Класс описания вращения (пока на плоскости xy)
/// </summary>
[Serializable]
public class primitivRotate
{
    //центр вращения
    public double X;  // координата в мм
    public double Y;  // координата в мм
    public double Z;  // координата в мм
    // шаг вращения
    public double angleStart;  // градус начала
    public double angleStop;   // градус окончания
    public double angleStep;   // градус шага
    public double radius;      // радиус окружности

    public double deltaStepRadius; // изменение радиуса с каждым шагом
    public double RotateRotates; //градус на который вращать данные

    public string Name;   // для представления

    public primitivRotate(double _x, double _y, double _z, double _angleStart, double _angleStop, double _angleStep, double _radius, double _deltaStepRadius, double _RotateRotates, string _name = "Вращение")
    {
        X = _x;
        Y = _y;
        Z = _z;

        angleStart = _angleStart;
        angleStop = _angleStop;
        angleStep = _angleStep;
        radius = _radius;
        deltaStepRadius = _deltaStepRadius;
        RotateRotates = _RotateRotates;

        Name = _name;
    }
}

/// <summary>
/// Класс описания цикла
/// </summary>
[Serializable]
public class primitivCycle
{
    public double cStart;       // начальное значение цикла
    public double cStop;       // конечное значение цикла
    public double cStep;       // шаг
    public string Name;   // для представления
    public bool AllowDeltaX; // необходимость применять цикл к оси X
    public bool AllowDeltaY; // необходимость применять цикл к оси Y
    public bool AllowDeltaZ; // необходимость применять цикл к оси Z

    public primitivCycle(double _cStart, double _cStop, double _cStep, bool _AllowDeltaX, bool _AllowDeltaY, bool _AllowDeltaZ, string _name = "Цикл")
    {
        cStart = _cStart;
        cStop = _cStop;
        cStep = _cStep;
        Name = _name;
        AllowDeltaX = _AllowDeltaX;
        AllowDeltaY = _AllowDeltaY;
        AllowDeltaZ = _AllowDeltaZ;
    }
}

/// <summary>
/// Примитив точка
/// </summary>
[Serializable]
public class primitivPoint
{
    public double X;       // координата в мм
    public double Y;       // координата в мм
    public double Z;       // координата в мм

    public primitivPoint(double _x, double _y, double _z)
    {
        X = _x;
        Y = _y;
        Z = _z;
    }
}

/// <summary>
/// Класс для хранения данных, конструктора
/// </summary>
[Serializable]
public class primitivNode
{
    public string GUID;
    public primitivType typeNode;

    public primitivCatalog catalog;
    public primitivPoint point;
    public primitivCycle cycler;
    public primitivRotate rotate;

    public List<primitivNode> nodes;

    public primitivNode()
    {
        GUID = "";
        typeNode = primitivType.catalog;

        catalog = null;
        point = null;
        cycler = null;
        rotate = null;

        nodes = new List<primitivNode>();
    }

    public primitivNode(primitivCatalog _catalog)
    {
        GUID = Guid.NewGuid().ToString();
        typeNode = primitivType.catalog;

        catalog = _catalog;
        point = null;
        cycler = null;
        rotate = null;

        nodes = new List<primitivNode>();
    }

    public primitivNode(primitivCycle _cycle)
    {
        GUID = Guid.NewGuid().ToString();
        typeNode = primitivType.cycler;

        catalog = null;    
        point = null;   
        cycler = _cycle;
        rotate = null;

        nodes = new List<primitivNode>();
    }

    public primitivNode(primitivPoint _point)
    {
        GUID = Guid.NewGuid().ToString();
        typeNode = primitivType.point;

        catalog = null;
        point = _point;
        cycler = null;
        rotate = null;

        nodes = new List<primitivNode>();
    }

    public primitivNode(primitivRotate _rotate)
    {
        GUID = Guid.NewGuid().ToString();
        typeNode = primitivType.rotate;

        catalog = null;
        point = null;
        cycler = null;
        rotate = _rotate;

        nodes = new List<primitivNode>();
    }
}