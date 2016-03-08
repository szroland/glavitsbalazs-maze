#define System_Drawing
#if UNITY_EDITOR
#undef System_Drawing
#endif
#if System_Drawing
using System.Drawing;
using System.Drawing.Imaging;
#endif
#if UNITY_EDITOR
using UnityEngine;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Maze
{
    [Serializable]
    public class ByteMaze
    {
        private byte[,] _maze_data;
        private List<GridPos> _sol;

        private int _width;
        private int _height;
        private int _sx;
        private int _sy;
        private int _seed;
        private int _sm;
        private GridPos _sou;
        private GridPos _dest;

        public byte[,] Raw { get { return _maze_data; } }
        public List<GridPos> Solution { get { return _sol; } }
        public bool IsSolved { get { return _sol != null; } }
        public bool? IsSolvable { get; private set; }
        public bool IsGenerated { get { return _maze_data != null; } }

        public int Sx { get { return _sx; } }
        public int Sy { get { return _sy; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int Seed { get { return _seed; } }
        public int Smoothness { get { return _sm; } }
        public GridPos Source { get { return _sou; } }
        public GridPos Destination { get { return _dest; } }

        public ByteMaze(uint Width, uint Height, int? Seed = null, int Smoothness = 0, GridPos Destination = null,
            GridPos Source = null, bool genarate = true, bool solve = true)
        {
            _width = (int)Width;
            _height = (int)Height;
            _sx = _width + 1;
            _sy = _height + 1;
            if (Seed == null)
                Seed = Environment.TickCount;
            if (Smoothness > 30)
                throw new ArgumentOutOfRangeException();
            if (Destination == null)
                Destination = new GridPos(_width - 1, _width - 1);
            else if (!Destination.IsValid(_sx, _sy))
                throw new ArgumentException();
            if (Source == null)
                Source = new GridPos(0, 0);
            else if (!Destination.IsValid(_sx, _sy))
                throw new ArgumentException();
            if (Source == Destination)
                throw new ArgumentException();
            _seed = Seed.Value;
            _sm = Smoothness;
            _sou = Source;
            _dest = Destination;
            if (genarate)
            {
                Generate();
                _sx = _maze_data.GetUpperBound(0);
                _sy = _maze_data.GetUpperBound(1);
                for (int i = 0; i < _maze_data.GetLength(0); i++)
                    _maze_data[i, _sy] = direction_hor;
                for (int i = 0; i < _maze_data.GetLength(1); i++)
                    _maze_data[_sx, i] = direction_ver;
                _maze_data[_sou.x, _sou.y] = direction_hor | direction_ver;
                _maze_data[_sou.x, _sou.y + 1] |= direction_ver;
                _maze_data[_sou.x + 1, _sou.y] |= direction_hor;
                _maze_data[_dest.x, _dest.y] = direction_hor | direction_ver;
                _maze_data[_dest.x, _dest.y + 1] |= direction_ver;
                _maze_data[_dest.x + 1, _dest.y] |= direction_hor;
            }
            if (solve) Solve();
        }

        public void Generate()
        {
            _maze_data = Generate((uint)_width, (uint)_height, _seed, (uint)_sm);
        }

        public void Solve()
        {
            IsSolvable = false;
            _sol = Solve(_maze_data, _sou, _dest);
            if (IsSolved) IsSolvable = true;
        }

        public byte this[int x, int y]
        {
            get { return _maze_data[x, y]; }
            private set { _maze_data[x, y] = value; }
        }

        public const byte direction_hor = 2;
        public const byte direction_ver = 1;
        public static List<GridPos> Solve(byte[,] _maze_data, GridPos source, GridPos dest)
        {
            return Solve(_maze_data, source.x, source.y, dest.x, dest.y);
        }
        public static List<GridPos> Solve(byte[,] _maze_data, int xSource, int ySource, int xDest, int yDest)
        {
            var sx = _maze_data.GetUpperBound(0);
            var sy = _maze_data.GetUpperBound(1);
            int[,] tMazePath = new int[sx, sy];
            bool destReached = false;
            GridPos cellPos = new GridPos(xSource, ySource);
            List<GridPos> calcState = new List<GridPos>();
            calcState.Add(cellPos);
            int step = 0;
            for (int i = 0; i < sx; i++)
                for (int j = 0; j < sy; j++)
                    tMazePath[i, j] = -1;
            tMazePath[xSource, ySource] = step;
            if (_maze_data == null) return null;
            if (xSource == xDest && ySource == yDest) return calcState;
            while (!destReached && calcState.Count > 0)
            {
                step++;
                List<GridPos> calcNextState = new List<GridPos>();
                for (int i = 0; i < calcState.Count; i++)
                {
                    GridPos calcCPos = calcState[i];
                    // N
                    if (calcCPos.y > 0)
                        if (tMazePath[calcCPos.x, calcCPos.y - 1] == -1)
                            if ((_maze_data[calcCPos.x, calcCPos.y] & direction_ver) != 0)
                            {
                                tMazePath[calcCPos.x, calcCPos.y - 1] = step;
                                GridPos calcNextCPos = new GridPos(calcCPos.x, calcCPos.y - 1);
                                calcNextState.Add(calcNextCPos);
                                destReached = calcNextCPos.x == xDest && calcNextCPos.y == yDest;
                            }
                    // W
                    if (calcCPos.x > 0)
                        if (tMazePath[calcCPos.x - 1, calcCPos.y] == -1)
                            if ((_maze_data[calcCPos.x, calcCPos.y] & direction_hor) != 0)
                            {
                                tMazePath[calcCPos.x - 1, calcCPos.y] = step;
                                GridPos calcNextCPos = new GridPos(calcCPos.x - 1, calcCPos.y);
                                calcNextState.Add(calcNextCPos);
                                destReached = calcNextCPos.x == xDest && calcNextCPos.y == yDest;
                            }
                    // S
                    if (calcCPos.y < sy - 1)
                        if (tMazePath[calcCPos.x, calcCPos.y + 1] == -1)
                            if ((_maze_data[calcCPos.x, calcCPos.y + 1] & direction_ver) != 0)
                            {
                                tMazePath[calcCPos.x, calcCPos.y + 1] = step;
                                GridPos calcNextCPos = new GridPos(calcCPos.x, calcCPos.y + 1);
                                calcNextState.Add(calcNextCPos);
                                destReached = calcNextCPos.x == xDest && calcNextCPos.y == yDest;
                            }
                    // E
                    if (calcCPos.x < sx - 1)
                        if (tMazePath[calcCPos.x + 1, calcCPos.y] == -1)
                            if ((_maze_data[calcCPos.x + 1, calcCPos.y] & direction_hor) != 0)
                            {
                                tMazePath[calcCPos.x + 1, calcCPos.y] = step;
                                GridPos calcNextCPos = new GridPos(calcCPos.x + 1, calcCPos.y);
                                calcNextState.Add(calcNextCPos);
                                destReached = calcNextCPos.x == xDest && calcNextCPos.y == yDest;
                            }
                }
                calcState = calcNextState;
            }
            if (destReached == false)
                return null;
            else
            {
                tMazePath[xDest, yDest] = step;
                List<GridPos> pPath = new List<GridPos>();
                int tx = xDest;
                int ty = yDest;
                pPath.Add(new GridPos(tx, ty));
                bool stepExists;
                while (tx != xSource || ty != ySource)
                {
                    step = tMazePath[tx, ty];
                    stepExists = false;
                    // N
                    if (ty > 0 && stepExists == false)
                        if (tMazePath[tx, ty - 1] == step - 1 &&
                             (_maze_data[tx, ty] & direction_ver) != 0
                           )
                        {
                            ty -= 1; stepExists = true;
                            pPath.Add(new GridPos(tx, ty));
                        }
                    // W	
                    if (tx > 0 && stepExists == false)
                        if (tMazePath[tx - 1, ty] == step - 1 &&
                             (_maze_data[tx, ty] & direction_hor) != 0
                           )
                        {
                            tx -= 1; stepExists = true;
                            pPath.Add(new GridPos(tx, ty));
                        }
                    // S	
                    if (ty < sy - 1 && stepExists == false)
                        if (tMazePath[tx, ty + 1] == step - 1 &&
                             (_maze_data[tx, ty + 1] & direction_ver) != 0
                           )
                        {
                            ty += 1; stepExists = true;
                            pPath.Add(new GridPos(tx, ty));
                        }
                    // E	
                    if (tx < sx - 1 && stepExists == false)
                        if (tMazePath[tx + 1, ty] == step - 1 &&
                             (_maze_data[tx + 1, ty] & direction_hor) != 0
                           )
                        {
                            tx += 1; stepExists = true;
                            pPath.Add(new GridPos(tx, ty));
                        }
                    if (stepExists == false) return null;
                }
                return pPath;
            }
        }
        public static byte[,] Generate(uint Width, uint Height, int? Seed = null, uint Smoothness = 0)
        {
            if (Smoothness > 30)
                throw new ArgumentException();
            if (Seed == null)
                Seed = Environment.TickCount;
            var sx = (int)Width;
            var sy = (int)Height;
            var _maze_base = new int[(sx + 1) * (sy + 1)];
            var _r = new Random(Seed.Value);
            var _stack = new List<CellState>();
            var data = new byte[sx + 1, sy + 1];
            var s = new CellState(_r.Next() % sx, _r.Next() % sy, 0);
            bool bEnd = false, found;
            int indexSrc, indexDest, tDir = 0, prevDir = 0;
            Func<int, int, int> _cell_index = (x, y) => sx * y + x;
            Func<int, int> _base_cell = (tIndex) =>
            {
                int index = tIndex;
                while (_maze_base[index] >= 0)
                {
                    index = _maze_base[index];
                }
                return index;
            };
            Action<int, int> _merge = (index1, index2) =>
            {
                int base1 = _base_cell(index1);
                int base2 = _base_cell(index2);
                _maze_base[base2] = base1;
            };
            for (int i = 0; i < sx + 1; i++)
            {
                for (int j = 0; j < sy + 1; j++)
                {
                    _maze_base[(sx + 1) * j + i] = -1;
                    data[i, j] = 0;
                }
            }
            while (true)
            {
                if (s.dir == 15)
                {
                    while (s.dir == 15)
                    {
                        if (_stack.Count > 0)
                        {
                            s = _stack[_stack.Count - 1];
                            _stack.RemoveAt(_stack.Count - 1);
                        }
                        else
                        {
                            bEnd = true;
                            break;
                        }
                    }
                    if (bEnd) break;
                }
                else
                {
                    do
                    {
                        prevDir = tDir;
                        tDir = (int)Math.Pow(2, _r.Next() % 4);
                        if ((_r.Next() % 32) < Smoothness)
                            if ((s.dir & prevDir) == 0)
                                tDir = prevDir;
                        found = (s.dir & tDir) != 0;
                    } while (found && s.dir != 15);
                    s.dir |= tDir;
                    indexSrc = _cell_index(s.x, s.y);
                    //W
                    if (tDir == 1 && s.x > 0)
                    {
                        indexDest = _cell_index(s.x - 1, s.y);
                        if (_base_cell(indexSrc) != _base_cell(indexDest))
                        {
                            _merge(indexSrc, indexDest);
                            data[s.x, s.y] |= direction_hor;
                            _stack.Add(new CellState(s));
                            s.x -= 1; s.dir = 0;
                        }
                    }
                    //E
                    if (tDir == 2 && s.x < sx - 1)
                    {
                        indexDest = _cell_index(s.x + 1, s.y);
                        if (_base_cell(indexSrc) != _base_cell(indexDest))
                        {
                            _merge(indexSrc, indexDest);
                            data[s.x + 1, s.y] |= direction_hor;
                            _stack.Add(new CellState(s));
                            s.x += 1; s.dir = 0;
                        }
                    }
                    //N
                    if (tDir == 4 && s.y > 0)
                    {
                        indexDest = _cell_index(s.x, s.y - 1);
                        if (_base_cell(indexSrc) != _base_cell(indexDest))
                        {
                            _merge(indexSrc, indexDest);
                            data[s.x, s.y] |= direction_ver;
                            _stack.Add(new CellState(s));
                            s.y -= 1; s.dir = 0;
                        }
                    }
                    //S
                    if (tDir == 8 && s.y < sy - 1)
                    {
                        indexDest = _cell_index(s.x, s.y + 1);
                        if (_base_cell(indexSrc) != _base_cell(indexDest))
                        {
                            _merge(indexSrc, indexDest);
                            data[s.x, s.y + 1] |= direction_ver;
                            _stack.Add(new CellState(s));
                            s.y += 1; s.dir = 0;
                        }
                    }
                }
            }
            return data;
        }
        private class CellState
        {
            public int x, y, dir;
            public CellState(int tx, int ty, int td) { x = tx; y = ty; dir = td; }
            public CellState(CellState s) { x = s.x; y = s.y; dir = s.dir; }
        }
    }

    [Serializable] //?
    public abstract class Maze<T>
        where T : MazeElement, new()
    {
        protected ByteMaze _data;
        protected T[,] _elements;

        public ByteMaze Data
        {
            get
            {
                if (_data == null)
                    throw new InvalidOperationException();
                else if (!_data.IsGenerated)
                    _data.Generate();
                return _data;
            }
        }

        public T[,] Elements
        {
            get
            {
                if (_elements == null)
                    LoadElements();
                return _elements;
            }
        }

        public T this[int x, int y]
        {
            get { return Elements[x, y]; }
            set { Elements[x, y] = value; }
        }

        public int Width { get { return Elements.GetLength(0); } }
        public int Height { get { return Elements.GetLength(1); } }

        public virtual void LoadElements()
        {
            _elements = GetElements(Data);
        }
        public static T[,] GetElements(ByteMaze m)
        {
            var res = new T[m.Width, m.Height];
            for (int x = 0; x < m.Width; x++)
            {
                for (int y = 0; y < m.Height; y++)
                {
                    var e = new T();
                    e.FromByteMaze(m, x, y);
                    res[x, y] = e;
                }
            }
            return res;
        }

        private static readonly BinaryFormatter _form = new BinaryFormatter();

        public void SaveToFile(string filename)
        {
            using (var s = File.OpenWrite(filename))
            {
                try
                {
                    _form.Serialize(s, this);
                }
                catch (Exception ex)
                {
                    //!GetType().IsSerializable
                    throw new Exception("ha ezt a kivételt kapod akkor átírtad a kódomat", ex);
                }
            }
        }

        public static Maze<T> LoadFromFile(string filename)
        {
            object r = null;
            using (var s = File.OpenRead(filename))
                r = _form.Deserialize(s);
            return r as Maze<T>;
        }
    }

    [Serializable] //?
    public abstract class MazeElement
    {
        public abstract void FromByteMaze(ByteMaze m, int x, int y);
    }

    [Serializable]
    public sealed class GridPos
    {
        public int x, y;
        public GridPos() { }
        public GridPos(int xp, int yp) { x = xp; y = yp; }
        public bool IsValid(int sx, int sy) { return x > sx && y > sy; }
        public static bool operator ==(GridPos a, GridPos b)
        {
            return Equals(a, null) || Equals(b, null) ?
                Equals(a, null) && Equals(b, null) :
                a.x == b.x && a.y == b.y;
        }
        public static bool operator !=(GridPos a, GridPos b) { return !(a == b); }
        public override bool Equals(object obj) { return (obj is GridPos) ? this == (obj as GridPos) : false; }
        public override int GetHashCode() { return base.GetHashCode(); }
    }

    [Serializable]
    public class CellMaze : Maze<Cell>
    {
        public CellMaze(ByteMaze data)
        {
            _data = data;
            LoadElements();
        }

        public CellMaze(uint Width, uint Height, int? Seed = null, int Smoothness = 0, GridPos Destination = null,
            GridPos Source = null, bool genarate = true, bool solve = true)
        {
            _data = new ByteMaze(Width, Height, Seed, Smoothness, Destination, Source, genarate, solve);
            LoadElements();
        }

        public void Draw(
            Action<float, float, float, float> line,
            float width, float height, float x = 0, float y = 0,
            bool fill_exits = false)
        {
            for (int i = 0; i < Data.Width; i++)
            {
                for (int j = 0; j < Data.Height; j++)
                {
                    var e = Elements[i, j];
                    float rx = width / Data.Width;
                    float ry = height / Data.Height;
                    if (e.N) line(x + rx * i, y + ry * j, x + rx * i, y + ry * (j + 1));
                    if (e.W) line(x + rx * i, y + ry * j, x + rx * (i + 1), y + ry * j);
                    if (e.S && i == Data.Width - 1)
                        line(x + rx * (i + 1), y + ry * j, x + rx * (i + 1), y + ry * (j + 1));
                    if (e.E && j == Data.Height - 1)
                        line(x + rx * i, y + ry * (j + 1), x + rx * (i + 1), y + ry * (j + 1));
                }
            }
        }

        #region System_Drawing
#if System_Drawing
        internal Bitmap DrawToBitmap(int? width = null, int? height = null, bool solution = true)
        {
            const float xa = 0;//50f;
            const float ya = 0;//50f;
            const int wa = 0;//100;
            const int ha = 0;//100;
            if (!width.HasValue) width = Width * 8;
            if (!height.HasValue) height = Height * 8;
            Bitmap r = new Bitmap(width.Value + 1 + wa, height.Value + 1 + ha);
            using (Graphics g = Graphics.FromImage(r))
            {
                g.FillRectangle(Brushes.White, 0, 0, width.Value + 1 + wa, height.Value + 1 + ha);
                Draw((x1, y1, x2, y2) => g.DrawLine(Pens.Blue, x1, y1, x2, y2),
                    width.Value, height.Value, xa, ya);
                if (Data.IsSolved && solution)
                {
                    float rx = (float)(width) / (Data.Width);
                    float ry = (float)(height) / (Data.Height);
                    for (int i = 0; i < Data.Solution.Count - 1; i++)
                    {
                        var c1 = Data.Solution[i];
                        var c2 = Data.Solution[i + 1];
                        g.DrawLine(Pens.Red,
                            c1.x * rx + rx / 2 + xa,
                            c1.y * ry + ry / 2 + ya,
                            c2.x * rx + rx / 2 + xa,
                            c2.y * ry + ry / 2 + ya);
                    }
                }
            }
            return r;
        }

        internal void DrawToFile(string bmp_file, int? width = null, int? height = null, bool solution = true)
        {
            if (!width.HasValue) width = Width * 8;
            if (!height.HasValue) height = Height * 8;
            bmp_file = Path.ChangeExtension(bmp_file, "bmp");
            using (var b = DrawToBitmap(width.Value, height.Value, solution))
                b.Save(bmp_file, ImageFormat.Bmp);
        }
#endif
        #endregion
    }

    [Serializable]
    public class Cell : MazeElement
    {
        public Wall N;
        public Wall W;
        public Wall S;
        public Wall E;

        public Cell(ByteMaze m, int x, int y)
        {
            FromByteMaze(m, x, y);
        }

        public override void FromByteMaze(ByteMaze m, int x, int y)
        {
            N = (m[x, y] & ByteMaze.direction_hor) == 0;
            W = (m[x, y] & ByteMaze.direction_ver) == 0;
            S = (m[x + 1, y] & ByteMaze.direction_hor) == 0;
            E = (m[x, y + 1] & ByteMaze.direction_ver) == 0;
        }

        public Cell(Wall n, Wall w, Wall s, Wall e)
        {
            N = n;
            W = w;
            S = s;
            E = e;
        }

        public Cell() : this(false, false, false, false) { }

        public override string ToString()
        {
            return string.Format("{0}: {1}, {2}, {3}, {4},", GetType().Name, N.IsSolid, W.IsSolid, S.IsSolid, E.IsSolid);
        }

        [Serializable]
        public class Wall
        {
            public bool IsSolid;

            public Wall(bool so)
            {
                IsSolid = so;
            }

            public Wall() : this(true) { }

            public override string ToString()
            {
                return string.Format("{0}: {1}", GetType().Name, IsSolid);
            }

            public static implicit operator bool(Wall b) { return b.IsSolid; }
            public static implicit operator Wall(bool b) { return new Wall(b); }
        }
    }

    [Serializable]
    public class BlockMaze : Maze<Block>
    {
        public BlockMaze(ByteMaze data)
        {
            _data = data;
            LoadElements();
            _elements[_data.Source.x, _data.Source.y] = false;
            //_elements[_data.Source.x + 1, _data.Source.y + 1] = false;
            _elements[_data.Destination.x, _data.Destination.y] = false;
            //_elements[_data.Destination.x + 1, _data.Destination.y + 1] = false;
        }

        public override void LoadElements()
        {
            _elements = new Block[Data.Width * 2 + 1, Data.Height * 2 + 1];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var e = new Block();
                    e.FromByteMaze(Data, x, y);
                    _elements[x, y] = e;
                }
            }
        }
        public BlockMaze(uint Width, uint Height, int? Seed = null, int Smoothness = 0, GridPos Destination = null,
            GridPos Source = null, bool genarate = true, bool solve = true)
            : this(new ByteMaze(Width / 2, Height / 2, Seed, Smoothness, Destination, Source, genarate, solve))
        { }

        public void Draw(Action<float, float> act,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false)
        {
            float rx = width / Width;
            float ry = height / Height;
            for (int xi = 0; xi < Width; xi++)
                for (int yi = 0; yi < Height; yi++)
                    if (Elements[xi, yi])
                        act(xi * rx + rx / 2f + x,
                            yi * ry + ry / 2f + y);
        }
        #region System_Drawing
#if System_Drawing
        internal Bitmap DrawToBitmap(int? width = null, int? height = null, bool solution = true)
        {
            const float xa = 0;//256f;
            const float ya = 0;//256f;
            const int wa = 0;// 512;
            const int ha = 0;//512;
            if (!width.HasValue) width = Width * 16;
            if (!height.HasValue) height = Height * 16;
            float rx = (float)(width.Value) / (Width);
            float ry = (float)(height.Value) / (Height);
            float w = (rx < ry ? rx : ry) / 8f;
            Bitmap r = new Bitmap(width.Value + wa, height.Value + ha);
            using (Graphics g = Graphics.FromImage(r))
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 230, 200)), 0, 0, width.Value + wa, height.Value + ha);
                var pl = new Pen(Color.DarkGreen, w);
                var bf = Brushes.LightGreen;
                Draw((x, y) =>
                {
                    x -= rx / 2f;
                    y -= ry / 2f;
                    g.FillRectangle(bf, x, y, rx, ry);
                    g.DrawRectangle(pl, x, y, rx, ry);
                }, width.Value, height.Value, xa, ya);

                if (Data.IsSolved && solution)
                {
                    var ps = new Pen(Color.Orange, w);
                    for (int i = 0; i < Data.Solution.Count - 1; i++)
                    {
                        var c1 = Data.Solution[i];
                        var c2 = Data.Solution[i + 1];
                        g.DrawLine(ps,
                            c1.x * 2f * ry + rx * 1.5f + xa,
                            c1.y * 2f * ry + ry * 1.5f + ya,
                            c2.x * 2f * rx + rx * 1.5f + xa,
                            c2.y * 2f * rx + ry * 1.5f + ya
                            );
                    }
                }
            }
            return r;
        }

        internal void DrawToFile(string bmp_file, int? width = null, int? height = null, bool solution = true)
        {
            if (!width.HasValue) width = Width * 16;
            if (!height.HasValue) height = Height * 16;
            bmp_file = Path.ChangeExtension(bmp_file, "bmp");
            using (var b = DrawToBitmap(width.Value, height.Value, solution))
                b.Save(bmp_file, ImageFormat.Bmp);
        }
#endif
        #endregion
    }

    [Serializable]
    public class Block : MazeElement
    {
        public const char block_on = '#';
        public const char block_off = ' ';

        public char Data;

        public bool IsSolid { get { return Data == block_on; } }
        public bool IsEmpy { get { return Data == block_off; } }

        public Block(bool so)
        {
            Data = so ? block_on : block_off;
        }
        public Block() : this(false) { }

        public static implicit operator bool(Block b) { return b.IsSolid; }
        public static implicit operator Block(bool b) { return new Block(b); }

        public override void FromByteMaze(ByteMaze m, int x, int y)
        {
            var xm = x % 2 == 0;
            var ym = y % 2 == 0;
            var b = m[x / 2, y / 2];
            var res = xm && ym;
            if (xm && !ym) res = (b & ByteMaze.direction_hor) == 0;
            if (!xm && ym) res = (b & ByteMaze.direction_ver) == 0;
            Data = res ? block_on : block_off;
        }
    }
}

namespace Maze
{
    class Demo
    {
        static void Main(string[] args)
        {
            ByteMaze b = new ByteMaze(30, 50);
            if (!b.IsSolvable.Value) throw new Exception(); // ez sajnos megtörténhet
            BlockMaze m = new BlockMaze(b);
            CellMaze c = new CellMaze(b);
            m.DrawToFile("blocks_output.bmp");
            c.DrawToFile("output.bmp");
        }
    }
}