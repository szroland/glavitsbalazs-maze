#if false
namespace KfgMazes
{
    [Serializable]
    public abstract class GridMaze<T>
        where T : GridMazeElement, new()
    {
        protected const byte direction_hor = GridMazeElement.direction_hor;
        protected const byte direction_ver = GridMazeElement.direction_ver;

        protected int _seed;
        protected GridPos _sou; //_sou != _dest
        protected GridPos _dest;

        protected byte[,] _maze_data;
        protected int _sx; // _maze_data.GetUpperBound(0)
        protected int _sy; // _maze_data.GetUpperBound(1)
        protected T[,] _elements;
        protected int _width; // _elements.GetUpperBound(0)
        protected int _height; // _elements.GetUpperBound(1)

        public virtual bool IsGenerated { get { return _maze_data != null; } }
        public virtual T this[int x, int y]
        {
            get
            {
                if (!IsGenerated)
                    throw new InvalidOperationException();
                if ((_elements[x, y] != null) ? (!_elements[x, y].IsValid) : true) // _element == null || !_element.IsValid
                {
                    _elements[x, y] = new T();
                    _elements[x, y]._data = _maze_data[x, y];
                    _elements[x, y]._plusx = _maze_data[x, y + 1];
                    _elements[x, y]._plusy = _maze_data[x + 1, y];
                }
                return _elements[x, y];
            }
            set
            {
                if (!IsGenerated)
                    throw new InvalidOperationException();
                if (value == null)
                    throw new ArgumentNullException();
                if (!value.IsValid)
                    throw new ArgumentException();
                _elements[x, y] = value;
                var v = value.GetData();
                _maze_data[x, y] = (byte)(v >> 0);
                _maze_data[x, y + 1] ^= (byte)((v >> 8));
                _maze_data[x + 1, y] ^= (byte)((v >> 16));
            }
        }
        public virtual T[,] InitalizeElements()
        {
            if (!IsGenerated) throw new InvalidOperationException();
            for (int x = 0; x < _elements.GetUpperBound(0); x++)
                for (int y = 0; y < _elements.GetUpperBound(1); y++)
                    if ((_elements[x, y] != null) ? (!_elements[x, y].IsValid) : true)
                    {
                        _elements[x, y] = new T();
                        _elements[x, y]._data = _maze_data[x, y];
                        _elements[x, y]._plusx = _maze_data[x, y + 1];
                        _elements[x, y]._plusy = _maze_data[x + 1, y];
                    }
            return _elements;
        }

        internal byte[,] MData { get { return _maze_data; } }
        internal int Sx { get { return _sx; } }
        internal int Sy { get { return _sy; } }
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public int Seed { get { return _seed; } }

        public abstract void Draw(Delegate act,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false);

        public abstract void Generate();

        public void Initalize(uint Width, uint Height, int? Seed = null, GridPos Destination = null, GridPos Source = null)
        {
            _width = (int)Width;
            _height = (int)Height;
            _sx = _width + 1;
            _sy = _height + 1;
            _elements = new T[_width, _height];
            // _maze_data = new byte[_sx, _sy];
            if (Seed == null)
                Seed = Environment.TickCount;
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
            _sou = Source;
            _dest = Destination;
        }

        private static readonly BinaryFormatter _form = new BinaryFormatter();

        public virtual void SaveToFile(string filename)
        {
            using (var s = File.OpenWrite(filename))
            {
                try
                {
                    _form.Serialize(s, this);
                }
                catch (Exception ex)
                {
                    throw new Exception("ha ezt a kivételt kapod akkor átírtad a kódomat", ex);
                }
            }
        }

        public static GridMaze<T> LoadFromFile(string filename)
        {
            object r = null;
            using (var s = File.OpenRead(filename))
                r = _form.Deserialize(s);
            return r as GridMaze<T>;
        }

#region System_Drawing
#if System_Drawing
        internal abstract Bitmap DrawToBitmap(int width = 512, int height = 512);

        internal void DrawToFile(string bmp_file, int width = 512, int height = 512)
        {
            bmp_file = Path.ChangeExtension(bmp_file, "bmp");
            using (var b = DrawToBitmap(width, height))
                b.Save(bmp_file, ImageFormat.Bmp);
        }
#endif
#endregion

    }

    [Serializable]
    public class GridMazeElement
    {
        //    __n__ _____
        //  w|_____|e
        //   |  s
        //   ^^^^^^^^^^^^^
        //   |data |plusx|
        //   |plusy|     |
        //
        //   data:
        //   |￣ = 3 = direction_ver | direction_hor
        //   |   = 2 = direction_hor
        //    ￣ = 1 = direction_ver
        //       = 0

        internal byte? _data;
        internal byte? _plusy;
        internal byte? _plusx;
        public const byte direction_hor = 2;
        public const byte direction_ver = 1;

        internal static int Comp(byte d, byte y, byte x)
        {
            int r = 0;
            r |= (d << 0);
            r |= ((y << 8) & direction_ver);
            r |= ((x << 16) & direction_hor);
            return r;
        }
        internal virtual int GetData()
        {
            byte rd = 0;
            byte ry = 0;
            byte rx = 0;
            if (IsValid)
            {
                rd = _data.Value;
                ry = _plusy.Value;
                rx = _plusx.Value;
            }
            else
                throw new InvalidOperationException();
            return Comp(rd, ry, rx);
        }
        internal virtual void SetData(int v)
        {
            _data = (byte)(v >> 0);
            _plusy ^= (byte)((v >> 8));
            _plusx ^= (byte)((v >> 16));
        }
        public void SetData(GridMazeElement v)
        {
            _data = v._data;
            _plusx = v._data;
            _plusy = v._data;
        }

        public virtual bool IsValid
        {
            get { return _data != null && _plusy != null && _plusx != null; }
        }

        public virtual bool TryMakeValid()
        {
            return IsValid;
        }

        public GridMazeElement() { }

        //erre szükség van egyáltalán?:
        public static GridMazeElement Create(byte[,] all_data, int x, int y)
        {
            if (x > all_data.GetUpperBound(0) || y > all_data.GetUpperBound(1))
                throw new IndexOutOfRangeException();
            var r = new GridMazeElement();
            r._data = all_data[x, y];
            r._plusx = all_data[x + 1, y];
            r._plusy = all_data[x, y + 1];
            return r;
        }
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

        //x^n+y^m=x^(n+(log(1+x^((m*log(y))/(log(x))-n)))/(log(x)))
    }

    [Serializable]
    public sealed class CellMaze : GridMaze<CellBase>
    {
        private int _sm;
        private List<GridPos> _sol;
        private CellMaze() { }

        public int Smoothness { get { return _sm; } }
        public GridPos Source { get { return _sou; } }
        public GridPos Destination { get { return _dest; } }
        public bool IsSolved { get { return _sol != null; } }
        public List<GridPos> Solution { get { if (!IsSolved) throw new InvalidOperationException(); return _sol; } }

        public CellMaze(uint Width, uint Height, GridPos Destination = null, GridPos Source = null, int? Seed = null, uint Smoothness = 0)
        {
            Initalize(Width, Height, Seed, Source, Destination, Smoothness);
        }

        public void Initalize(uint Width, uint Height, int? Seed = null, GridPos Destination = null, GridPos Source = null, uint Smoothness = 0)
        {
            if (Smoothness > 30)
                throw new ArgumentException();
            _sm = (int)Smoothness;
            base.Initalize(Width, Height, Seed, Destination, Source);
        }

        public override void Draw(Delegate act,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false)
        {
            if (!IsGenerated) throw new InvalidOperationException();
            Draw(_maze_data, _sou, _dest,
                new Action<float, float, float, float>((x1, y1, x2, y2) => act.DynamicInvoke(x1, y1, x2, y2)),
                width, height, x, y, fill_exits);
        }
        public void Draw(Action<float, float, float, float> line,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false)
        {
            if (!IsGenerated) throw new InvalidOperationException();
            Draw(_maze_data, _sou, _dest, line, width, height, x, y, fill_exits);
        }
        public override void Generate()
        {
            _maze_data = Generate((uint)_width, (uint)_height, _seed, (uint)_sm);
            _sx = _maze_data.GetUpperBound(0);
            _sy = _maze_data.GetUpperBound(1);
        }
        public void Solve()
        {
            if (!IsGenerated) throw new InvalidOperationException();
            _sol = Solve(_maze_data, _sou, _dest);
        }
        public static List<GridPos> Solve(byte[,] _maze_data, GridPos source, GridPos dest)
        {
            return Solve(_maze_data, source.x, source.y, dest.x, dest.y);
        }

#region System_Drawing
#if System_Drawing
        internal override Bitmap DrawToBitmap(int width = 512, int height = 512)
        {
            return DrawToBitmap(_maze_data, width, height, _sol, true, _sou, _dest);
        }
        internal void DrawToFile(string bmp_file, int width = 512, int height = 512, bool solution = true)
        {
            DrawToFile(bmp_file, _maze_data, width, height, _sol, solution, _sou, _dest);
        }
#endif
#endregion

#region algs
#region System_Drawing
#if System_Drawing
        private static Bitmap DrawToBitmapOld(byte[,] maze_data, int width = 512, int height = 512,
            List<GridPos> _sol = null, bool solution = true)
        {
            if (maze_data == null) throw new InvalidOperationException();
            Bitmap r = new Bitmap(width + 1, height + 1);
            var _sx = maze_data.GetUpperBound(0);
            var _sy = maze_data.GetLowerBound(1);
            float rx = width / _sx;
            float ry = height / _sy;
            using (Graphics g = Graphics.FromImage(r))
            {
                g.FillRectangle(Brushes.White, 0, 0, width, height);
                g.DrawRectangle(Pens.Blue, 0, 0, width, height);
                for (int i = 0; i < _sx; i++)
                {
                    for (int j = 0; j < _sy; j++)
                    {
                        var d = maze_data[i, j];
                        if ((d & 1) == 0)
                            g.DrawLine(Pens.Blue,
                                rx * i, ry * j,
                                rx * (i + 1), ry * j);
                        if ((d & 2) == 0)
                            g.DrawLine(Pens.Blue,
                                rx * i, ry * j,
                                rx * i, ry * (j + 1));
                    }
                }
                g.DrawLine(Pens.Red, 0, 0, rx, 0);
                g.DrawLine(Pens.Red, 0, 0, 0, rx);
                g.DrawLine(Pens.Red, width, height, width - rx, height);
                g.DrawLine(Pens.Red, width, height, width, height - ry);
                if (_sol != null && solution)
                {
                    for (int i = 0; i < _sol.Count - 1; i++)
                    {
                        var c1 = _sol[i];
                        var c2 = _sol[i + 1];
                        g.DrawLine(Pens.Red,
                            c1.x * rx + rx / 2, c1.y * ry + ry / 2,
                            c2.x * rx + rx / 2, c2.y * ry + ry / 2);
                    }
                }
            }
            return r;
        }

        internal static Bitmap DrawToBitmap(byte[,] maze_data, int width = 512, int height = 512,
            List<GridPos> _sol = null, bool solution = true, GridPos sou = null, GridPos dest = null)
        {
            var _sx = maze_data.GetUpperBound(0);
            var _sy = maze_data.GetUpperBound(1);
            if (dest == null)
            {
                if (_sol == null)
                    dest = new GridPos(_sx - 1, _sy - 1);
                else
                    dest = _sol[0];
            }
            if (sou == null)
            {
                if (_sol == null)
                    sou = new GridPos(0, 0);
                else
                    sou = _sol[0];
            }
            if (!dest.IsValid(_sx, _sy))
                dest = new GridPos(_sx - 1, _sy - 1);
            if (!sou.IsValid(_sx, _sy))
                sou = new GridPos(0, 0);
            const float xa = 0;//50f;
            const float ya = 0;//50f;
            const int wa = 0;//100;
            const int ha = 0;//100;
            Bitmap r = new Bitmap(width + 1 + wa, height + 1 + ha);
            using (Graphics g = Graphics.FromImage(r))
            {
                g.FillRectangle(Brushes.White, 0, 0, width + 1 + wa, height + 1 + ha);
                Draw(maze_data, sou, dest, (x1, y1, x2, y2) => g.DrawLine(Pens.Blue, x1, y1, x2, y2),
                    width, height, xa, ya);
                if (_sol != null && solution)
                {
                    float rx = (float)(width) / (_sx);
                    float ry = (float)(height) / (_sy);
                    for (int i = 0; i < _sol.Count - 1; i++)
                    {
                        var c1 = _sol[i];
                        var c2 = _sol[i + 1];
                        g.DrawLine(Pens.Red,
                            c1.x * rx + rx / 2 + xa, c1.y * ry + ry / 2 + ya,
                            c2.x * rx + rx / 2 + xa, c2.y * ry + ry / 2 + ya);
                    }
                }
            }
            return r;
        }

        internal static void DrawToFile(string bmp_file, byte[,] maze_data, int width = 512, int height = 512,
            List<GridPos> _sol = null, bool solution = true, GridPos sou = null, GridPos dest = null)
        {
            bmp_file = Path.ChangeExtension(bmp_file, "bmp");
            using (var b = DrawToBitmap(maze_data, width, height, _sol, solution, sou, dest))
                b.Save(bmp_file, ImageFormat.Bmp);
        }
#endif
#endregion

        public static void Draw(byte[,] maze_data, GridPos sou, GridPos dest,
            Action<float, float, float, float> line, float width, float height,
            float x = 0, float y = 0, bool fill_exits = false)
        {
            var data = (byte[,])maze_data.Clone();
            var sx = data.GetUpperBound(0);
            var sy = data.GetUpperBound(1);
            if ((sou != null && dest != null) ? (sou != dest && sou.IsValid(sx, sy) && dest.IsValid(sx, sy)) : true)
                throw new ArgumentException();
            float rx = width / sx;
            float ry = height / sy;
            Action<float, float, float, float> del = (x1, y1, x2, y2) =>
            {
                if (x1 != x2 || y1 != y2)
                    line(x1, y1, x2, y2);
                else return;
            };
            if (x != 0 || y != 0) del = (x1, y1, x2, y2) =>
            {
                if (x1 != x2 || y1 != y2)
                    line(x + x1, y + y1, x + x2, y + y2);
                else return;
            };

            for (int i = 0; i < sx + 1; i++)
                data[i, 0] |= direction_ver;
            for (int i = 0; i < sy + 1; i++)
                data[0, i] |= direction_hor;
            for (int i = 0; i < sx + 1; i++)
                data[i, sy] |= direction_ver;
            for (int i = 0; i < sy + 1; i++)
                data[sx, i] |= direction_hor;

            //draw inner walls:
            for (int i = 0; i < sx + 1; i++)
            {
                for (int j = 0; j < sy + 1; j++)
                {
                    var d = data[i, j];
                    if ((d & direction_ver) == 0)
                        del(rx * i, ry * j,
                            rx * (i + 1), ry * j);
                    if ((d & direction_hor) == 0)
                        del(rx * i, ry * j,
                            rx * i, ry * (j + 1));
                }
            }
            if (fill_exits)
            {
                del(0, 0, 0, height);
                del(0, 0, width, 0);
                del(width, 0, width, height);
                del(0, height, width, height);
                return;
            }

            //draw outer walls with exits:

#region shorthands
            var twidth = sx - 1;
            var theight = sy - 1;
            bool sou_on_edge = sou.x == 0 || sou.y == 0 || sou.x == twidth || sou.y == theight;
            bool dest_on_edge = dest.x == 0 || dest.y == 0 || dest.x == twidth || dest.y == theight;
            Action l = () => del(0, 0, 0, height);
            Action t = () => del(0, 0, width, 0);
            Action r = () => del(width, 0, width, height);
            Action b = () => del(0, height, width, height);
            Action<GridPos> leftexit = (c) =>
            {
                del(0, 0, 0, c.y * ry);
                del(0, c.y * ry + ry, 0, height);
            };
            Action<GridPos> topexit = (c) =>
            {
                del(0, 0, c.x * rx, 0);
                del(0, c.x * rx + rx, width, 0);
            };
            Action<GridPos> rightexit = (c) =>
            {
                del(width, 0, width, c.y * ry);
                del(width, c.y * ry + ry, width, height);
            };
            Action<GridPos> bottomexit = (c) =>
            {
                del(0, height, c.x * rx, height);
                del(c.x * rx + rx, height, width, height);
            };
            Action leftexits = () =>
            {
                GridPos c = null, f = null;
                if (sou.y < dest.y)
                {
                    c = sou; f = dest;
                }
                if (sou.y > dest.y)
                {
                    c = dest; f = sou;
                }
                del(0, 0, 0, c.y * ry);
                del(0, c.y * ry + ry, 0, f.y * ry);
                del(0, f.y * ry + ry, 0, height);
            };
            Action topexits = () =>
            {
                GridPos c = null, f = null;
                if (sou.x < dest.x)
                {
                    c = sou; f = dest;
                }
                if (sou.x > dest.x)
                {
                    c = dest; f = sou;
                }
                del(0, 0, c.x * rx, 0);
                del(c.x * rx + rx, 0, f.x * rx, 0);
                del(f.x * rx + rx, 0, width, 0);
            };
            Action rightexits = () =>
            {
                GridPos c = null, f = null;
                if (sou.y < dest.y)
                {
                    c = sou; f = dest;
                }
                if (sou.y > dest.y)
                {
                    c = dest; f = sou;
                }
                del(width, 0, width, c.y * ry);
                del(width, c.y * ry + ry, width, f.y * ry);
                del(width, f.y * ry + ry, width, height);
            };
            Action bottomexits = () =>
            {
                GridPos c = null, f = null;
                if (sou.x < dest.x)
                {
                    c = sou; f = dest;
                }
                if (sou.x > dest.x)
                {
                    c = dest; f = sou;
                }
                del(0, height, c.x * rx, height);
                del(c.x * rx + rx, height, f.x * rx, height);
                del(f.x * rx + rx, height, width, height);
            };
#endregion

            if (sou_on_edge)
            {
                if (dest_on_edge) //both on edge
                {
                    if (sou.x == 0 && dest.x == 0)//left both
                    {
                        leftexits();
                        t(); r(); b();
                    }
                    else if (sou.y == 0 && dest.y == 0)//top both
                    {
                        topexits();
                        l(); r(); b();
                    }
                    else if (sou.x == twidth && dest.x == twidth)//right both
                    {
                        rightexits();
                        l(); t(); b();
                    }
                    else if (sou.y == theight && dest.y == theight)//bottom both
                    {
                        bottomexits();
                        l(); t(); r();
                    }
                    else //exits are on different sides
                    {
                        int w = 0;
                        if (sou.x == 0)//left
                        {
                            leftexit(sou); w |= 1;
                        }
                        else if (sou.y == 0)//top 
                        {
                            topexit(sou); w |= 2;
                        }
                        else if (sou.x == twidth)//right
                        {
                            rightexit(sou); w |= 4;
                        }
                        else if (sou.y == theight)//bottom 
                        {
                            bottomexit(sou); w |= 8;
                        }
                        if (dest.x == 0)//left
                        {
                            leftexit(dest); w |= 1;
                        }
                        else if (dest.y == 0)//top 
                        {
                            topexit(dest); w |= 2;
                        }
                        else if (dest.x == twidth)//right
                        {
                            rightexit(dest); w |= 4;
                        }
                        else if (dest.y == theight)//bottom 
                        {
                            bottomexit(dest); w |= 8;
                        }
                        if ((w & 1) == 0) l();
                        if ((w & 2) == 0) t();
                        if ((w & 4) == 0) r();
                        if ((w & 8) == 0) b();
                    }
                }
                else //only sou on edge
                {
                    if (sou.x == 0)//left
                    {
                        leftexit(sou);
                        t(); r(); b();
                    }
                    if (sou.y == 0)//top 
                    {
                        topexit(sou);
                        l(); r(); b();
                    }
                    if (sou.x == twidth)//right
                    {
                        rightexit(sou);
                        l(); t(); b();
                    }
                    if (sou.y == theight)//bottom 
                    {
                        bottomexit(sou);
                        l(); t(); r();
                    }
                }
            }
            else if (dest_on_edge) //only dest on edge
            {
                if (dest.x == 0)//left
                {
                    leftexit(dest);
                    t(); r(); b();
                }
                if (dest.y == 0)//top 
                {
                    topexit(dest);
                    l(); r(); b();
                }
                if (dest.x == twidth)//right
                {
                    rightexit(dest);
                    l(); t(); b();
                }
                if (dest.y == theight)//bottom 
                {
                    bottomexit(dest);
                    l(); t(); r();
                }
            }
            else //fill exits
            {
                l(); t(); r(); b();
            }
        }

        //private static void DrawInner(byte[,] maze_data, Action<float, float, float, float> line,
        //    float width, float height, float x = 0, float y = 0)
        //{

        //}

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
            for (int i = 0; i < data.GetLength(0); i++)
                data[i, data.GetUpperBound(1)] = direction_hor;
            for (int i = 0; i < data.GetLength(1); i++)
                data[data.GetUpperBound(0), i] = direction_ver;
            return data;
        }
        private class CellState
        {
            public int x, y, dir;
            public CellState(int tx, int ty, int td) { x = tx; y = ty; dir = td; }
            public CellState(CellState s) { x = s.x; y = s.y; dir = s.dir; }
        }
#endregion
    }

    [Serializable]
    public class CellBase : GridMazeElement
    {
        protected WallBase _n;
        protected WallBase _w;
        protected WallBase _s;
        protected WallBase _e;

        public WallBase N
        {
            get
            {
                if (_n == null)
                    if (_data != null)
                        _n = new WallBase((_data & direction_ver) == 0);
                    else
                        throw new InvalidOperationException();
                return _n;
            }
            set
            {
                _n = value;
                if (_data != null)
                    if (value)
                        _data |= direction_ver;
            }
        }
        public WallBase W
        {
            get
            {
                if (_w == null)
                    if (_data != null)
                        _w = new WallBase((_data & direction_hor) == 0);
                    else
                        throw new InvalidOperationException();
                return _w;
            }
            set
            {
                _w = value;
                if (_data != null)
                    if (value)
                        _data |= direction_hor;
            }
        }
        public WallBase S
        {
            get
            {
                if (_s == null)
                    if (_plusy != null)
                        _s = new WallBase((_plusy & direction_ver) == 0);
                    else
                        throw new InvalidOperationException();
                return _s;
            }
            set
            {
                _s = value;
                if (_plusy != null)
                    if (value)
                        _plusy |= direction_ver;
            }
        }
        public WallBase E
        {
            get
            {
                if (_e == null)
                    if (_plusx != null)
                        _e = new WallBase((_plusx & direction_hor) == 0);
                    else
                        throw new InvalidOperationException();
                return _e;
            }
            set
            {
                _e = value;
                if (_plusx != null)
                    if (value)
                        _plusx |= direction_hor;
            }
        }

        public override bool IsValid
        {
            get
            {
                return base.IsValid || _n != null && _w != null && _s != null && _s != null;
            }
        }

        internal override int GetData()
        {
            byte rd = 0;
            byte ry = 0;
            byte rx = 0;
            if (_data != null && _plusy != null && _plusx != null)
            {
                rd = _data.Value;
                ry = _plusy.Value;
                rx = _plusx.Value;
            }
            else if (_n != null && _w != null && _s != null && _s != null)
            {
                if (!_n) rd |= direction_ver;
                if (!_w) rd |= direction_hor;
                if (!_s) ry |= direction_ver;
                if (!_e) rx |= direction_hor;
            }
            else
                throw new InvalidOperationException();
            return Comp(rd, ry, ry);
        }

        internal CellBase(byte[,] all_data, int x, int y)
        {
            if (x > all_data.GetUpperBound(0) || y > all_data.GetUpperBound(1))
                throw new IndexOutOfRangeException();
            _n = (all_data[x, y] & direction_hor) == 0;
            _w = (all_data[x, y] & direction_ver) == 0;
            _s = (all_data[x + 1, y] & direction_hor) == 0;
            _e = (all_data[x, y + 1] & direction_ver) == 0;
            _data = all_data[x, y];
            _plusx = all_data[x + 1, y];
            _plusy = all_data[x, y + 1];
        }

        public CellBase(WallBase n, WallBase w, WallBase s, WallBase e)
        {
            N = n;
            W = w;
            S = s;
            E = e;
        }

        public CellBase()
        {

        }

        public override string ToString()
        {
            return string.Format("{0}: {1}, {2}, {3}, {4},", GetType().Name, N.IsSolid, W.IsSolid, S.IsSolid, E.IsSolid);
        }

        [Serializable]
        public class WallBase
        {
            public bool IsSolid;

            public WallBase(bool so)
            {
                IsSolid = so;
            }

            public WallBase() { IsSolid = true; }

            public override string ToString()
            {
                return string.Format("{0}: {1}", GetType().Name, IsSolid);
            }

            public static implicit operator bool(WallBase b) { return b.IsSolid; }
            public static implicit operator WallBase(bool b) { return new WallBase(b); }
        }
    }

    [Serializable]
    public sealed class BlockMaze : GridMaze<BlockBase>, BlockMazeGenerator
    {
        private const byte block_on = BlockBase.block_on;
        private const byte block_off = BlockBase.block_off;

        // 1 cell byte => 4 bools = 4 bytes
        // o = true
        //
        //    |￣      =>   o t   =   o 1
        //                  t f       1 0

#warning TODO : megoldás

        public BlockMaze(uint Width, uint Height, int? Seed = null)
        {
            Initalize(Width, Height, Seed);
        }

        public override void Draw(Delegate act,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false)
        {
            Draw(new Action<float, float>((xr, yr) => act.DynamicInvoke(xr, yr)),
                width, height, x, y, fill_exits);
        }
        public void Draw(Action<float, float> act,
            float width = 512, float height = 512, float x = 0, float y = 0, bool fill_exits = false)
        {
            if (!IsGenerated)
                throw new InvalidOperationException();
            float rx = (float)width / _sx;
            float ry = (float)height / _sx;
            for (int xi = 0; xi < _sx; xi++)
                for (int yi = 0; yi < _sy; yi++)
                    if (_maze_data[xi, yi] == block_on)
                        act(xi * rx + rx / 2f + x,
                            yi * ry + ry / 2f + y);
        }

#warning TODO : rendes oda-vissza konvertálás
        public override void Generate()
        {
            //itt kicsit csalok ¯\_(ツ)_/¯
            ConvertToThis(new CellMaze((uint)(_width / 2), (uint)(_height / 2), null, null, _seed, 0));
        }
        public static BlockMaze Convert(CellMaze cm)
        {
            var res = new BlockMaze((uint)cm.Width, (uint)cm.Height, cm.Seed);
            res.ConvertToThis(cm);
            return res;
        }
        public void ConvertToThis(CellMaze cm)
        {
            cm.Generate();
            var cd = cm.MData;
            var cx = cm.Sx;
            var cy = cm.Sy;
            _width = cx * 2;
            _height = cy * 2;
            _sx = _width + 1;
            _sy = _height + 1;
            _seed = cm.Seed;
            _elements = new BlockBase[_width, _height];
            _maze_data = new byte[_sx, _sy];
            for (int x = 0; x < cx; x++)
            {
                for (int y = 0; y < cy; y++)
                {
                    var d = cd[x, y];
                    _maze_data[x * 2, y * 2] = block_on;
                    _maze_data[x * 2, y * 2 + 1] = ((d & direction_hor) == direction_hor) ? block_off : block_on;
                    _maze_data[x * 2 + 1, y * 2] = ((d & direction_ver) == direction_ver) ? block_off : block_on;
                }
            }
            for (int i = 0; i < _sx; i++)
                _maze_data[i, _sy - 1] = block_on;
            for (int i = 0; i < _sy; i++)
                _maze_data[_sx - 1, i] = block_on;
        }

        public void SaveToTxtFile(string txt_file)
        {
            SaveToTxtFile<BlockBase>(txt_file);
        }
        public void SaveToTxtFile<T>(string txt_file)
            where T : BlockBase, new()
        {
            Path.ChangeExtension(txt_file, "txt");
            var dum = new T();
            var nb = dum.NewBlockString;
            var nl = dum.NewLineString;
            var off = dum.OffString;
            var on = dum.OnString;
            if (nb == null)
            {
                if (off.Length != 1 || on.Length != 1)
                    throw new NotSupportedException(); //valószínüleg nem történik meg
            }
            if (string.IsNullOrEmpty(nl) || string.IsNullOrEmpty(on) || string.IsNullOrEmpty(off) || nb == string.Empty)
                throw new NotSupportedException();
            using (var s = new StreamWriter(txt_file))
            {
                for (int x = 0; x < _width; x++)
                {
                    for (int y = 0; y < _height; y++)
                    {
                        GridMazeElement g = this[y, x];
                        T val = g as T != null ? g as T : new T();
                        val.SetData(g);
                        var d = val.GetFileSaveString();
                        if (nb != null)
                        {
                            s.Write(d);
                            s.Write(nb);
                        }
                        else
                        {
                            if (d.Length != 1)
                                throw new NotSupportedException();
                            s.Write(d);
                        }
                    }
#warning TODO : kijáratok
                    //^ a rajzoló metódusban is!
                    s.Write(on);
                    s.Write(nl);
                }
                for (int i = 0; i < _width + 1; i++)
                    s.Write(on);
            }
        }
        public static BlockMaze LoadFromTxtFile(string txt_file, bool edge = false)
        {
            return LoadFromTxtFile<BlockBase>(txt_file, edge);
        }

        //ez a metódus nem nézi hogy hibás e a fájl
        public static BlockMaze LoadFromTxtFile<T>(string txt_file, bool edge = false)
            where T : BlockBase, new()
        {
            var dum = new T();
            var nb = dum.NewBlockString;
            var nl = dum.NewLineString;
            var off = dum.OffString;
            var on = dum.OnString;
            if (nb == null)
            {
                if (off.Length != 1 || on.Length != 1)
                    throw new NotSupportedException(); //valószínüleg nem történik meg
            }
            if (string.IsNullOrEmpty(nl) || string.IsNullOrEmpty(on) || string.IsNullOrEmpty(off) || nb == string.Empty)
                throw new NotSupportedException();
            string[] lines = null;
            using (var s = File.OpenText(txt_file))
            {
                var all = s.ReadToEnd();
                lines = all.Split(new string[] { nl }, StringSplitOptions.None);
                all = null;
            }
            T[,] els = null;
            var sx = 0;
            var sy = 0;
            var width = 0;
            var height = 0;
            if (nb == null)
            {
                foreach (var item in lines)
                    if (item.Length > sx)
                        sx = item.Length;
                sy = lines.Length;
                width = edge ? sx - 1 : sx;
                height = edge ? sy - 1 : sy;
                els = new T[width, height];
                for (int y = 0; y < height; y++)
                {
                    var line = lines[y];
                    for (int x = 0; x < width; x++)
                    {
                        var c = line.Length > x ? line[x].ToString() : off;
                        els[x, y] = new T();
                        els[x, y].SetString(c);
                    }
                }
            }
            else
            {
                sy = lines.Length;
                string[][] blocks = new string[sy][];
                for (int y = 0; y < sy; y++)
                {
                    blocks[y] = lines[y].Split(new string[] { nb }, StringSplitOptions.None);
                    if (blocks[y].Length > sx)
                        sx = blocks[y].Length;
                }
                width = sx - 1;
                height = sy - 1;
                els = new T[width, height];
                for (int y = 0; y < height; y++)
                {
                    Array.Resize(ref blocks[y], sx);
                    for (int x = 0; x < width; x++)
                    {
                        if (blocks[y][x] == null)
                            blocks[y][x] = off;
                        els[x, y] = new T();
                        els[x, y].SetString(blocks[x][y]);
                    }
                }
            }
            lines = null;

            var res = new BlockMaze((uint)sx, (uint)sy);
            res._maze_data = new byte[sx, sy];
            res._elements = (T[,])els.Clone();
            res._sx = sx;
            res._sy = sy;
            res._width = sx - 1;
            res._height = sy - 1;
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    res[x, y] = els[x, y];
            if (edge) //rögtönzött megoldás
            {
                for (int i = 0; i < sx; i++)
                    res._maze_data[i, sy - 1] = block_on;
                for (int i = 0; i < sy; i++)
                    res._maze_data[sx - 1, i] = block_on;
            }
            return res;
        }

#region github kompatibilis 
        public BlockMaze generate()
        {
            Generate();
            return this;
        }

        public BlockMaze(bool[,] maze)
        {
            var sx = maze.GetLength(0);
            var sy = maze.GetLength(1);
            Initalize((uint)sx, (uint)sy);
            _maze_data = new byte[sx + 1, sy + 1];
            for (int x = 0; x < sx; x++)
                for (int y = 0; y < sy; y++)
                    this[x, y] = maze[x, y];
            //for (int i = 0; i < sx; i++)
            //    _maze_data[i, sy - 1] = block_on;
            //for (int i = 0; i < sy; i++)
            //    _maze_data[sx - 1, i] = block_on;
        }

        public bool Fal(int x, int y)
        {
            return this[x, y];
        }

        public int MeretX
        {
            get { return _elements.GetLength(0); }
        }

        public int MeretY
        {
            get { return _elements.GetLength(1); }
        }
#endregion

#region System_Drawing
#if System_Drawing

        //new SolidBrush(Color.FromArgb(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255)))

        internal override Bitmap DrawToBitmap(int width = 512, int height = 512)
        {
            const float xa = 0;//256f;
            const float ya = 0;// 256f;
            const int wa = 0;// 512;
            const int ha = 0;// 512;
            float rx = (float)(width) / (_sx);
            float ry = (float)(height) / (_sx);
            Bitmap r = new Bitmap(width + wa, height + ha);
            using (Graphics g = Graphics.FromImage(r))
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(200, 230, 200)), 0, 0, width + wa, height + ha);
                Draw((x, y) =>
                {
                    x -= rx / 2f;
                    y -= ry / 2f;
                    g.FillRectangle(Brushes.LightGreen,
                        x, y, rx, ry);
                    g.DrawRectangle(new Pen(Color.DarkGreen, (rx < ry ? rx : ry) / 8f),
                        x, y, rx, ry);
                }, width, height, xa, ya);
            }
            return r;
        }

#endif
#endregion
    }

    //kompatiblis a githubos interface-szel
    public interface BlockMazeGenerator
    {
        BlockMaze generate();
    }

    [Serializable]
    public class BlockBase : GridMazeElement
    {
        public const byte block_on = 1;
        public const byte block_off = 0;
        public const string file_default_newline = "\r\n"; //csak ezért string nem char
        public const string file_default_newblock = null; //ez egy rögtönzött megoldás
        public const string file_default_on = "#";
        public const string file_default_off = " ";

        public bool IsSolid
        {
            get { return _data != null ? _data.Value == block_on : false; }
            set { _data = value ? block_on : block_off; }
        }

        public BlockBase(bool so)
        {
            _data = so ? block_on : block_off;
            _plusy = 0;
            _plusx = 0;
        }

        public BlockBase() : this(true) { }

        public override string ToString()
        {
            return string.Format("{0}: {1}", GetType().Name, IsSolid);
        }

        public virtual string GetFileSaveString()
        {
            return IsSolid ? OnString : OffString;
        }

        public virtual void SetString(string c)
        {
            IsSolid = c == OnString;
        }

        public virtual string OnString { get { return file_default_on; } }
        public virtual string OffString { get { return file_default_off; } }
        public virtual string NewLineString { get { return file_default_newline; } }
        public virtual string NewBlockString { get { return file_default_newblock; } }

        public static implicit operator bool(BlockBase b) { return b.IsSolid; }
        public static implicit operator BlockBase(bool b) { return new BlockBase(b); }
    }

    class Demo
    {
        [Serializable] //ez kell!
        public class Cell : CellBase
        {
            public Cell(byte[,] all_data, int x, int y)
                : base(all_data, x, y)
            { }
            public Cell(WallBase n, WallBase w, WallBase s, WallBase e)
                : base(n, w, s, e)
            { }
            internal Cell(byte? data, byte? plusx, byte? plusy)
                : base()
            { }
            //^^^ ezek nem szükségesek de így kényelmesebb

            //bővíthető:
        }

        [Serializable] //ez kell!
        public class Wall : CellBase.WallBase
        {
            public Wall(bool so) : base(so)
            { }

            //bővíthető:
        }

        [Serializable] //ez kell!
        public class Block : BlockBase
        {
            public Block()
                : base(true)
            { k = new Kulcs(false); }
            public Block(bool so)
                : base(so)
            { k = new Kulcs(false); }

            //bővíthető:

            public Kulcs k; //bool, ebben a kockában van-e kulcs?

            public Block(bool so, bool vanekulcs)
                : base(so)
            { k = new Kulcs(vanekulcs && !so); }

            public override string GetFileSaveString()
            {
                return IsSolid ? OnString : (k != null ? (k.vane ? "K" : OffString) : OffString);
                //ha ez a kocka üres és van benne kulcs akkor fájlba mentésnél "K" betűt írunk "." helyett
            }

            public override void SetString(string c)
            {
                base.SetString(c);
                k = new Kulcs(c == "K");
                //ezt a metódust is felül kell írni hogy be is lehessen tölteni a fájlt
            }

            //ha esetleg nem tetszene a '#' kocka karaktenek:
            //jobb lenne a statikus, de az nem lehetséges
            public override string OnString { get { return "@"; } }
            public override string OffString { get { return "."; } }
            public override string NewLineString { get { return "|"; } }
            //public override string NewBlockString { get { return "*"; } } //ez akkor kell ha a fenti stringek nem egy karakter hosszúak!

            [Serializable] //ez kell!
            public class Kulcs //csak példa osztály, nem igazi
            {
                public bool vane;
                public Kulcs(bool vane)
                {
                    this.vane = vane;
                }
            }
        }

        static void DemoM()
        {
            //cella labirintus:
            CellMaze m = new CellMaze(64, 64);
            m.Generate();
            var v = m.InitalizeElements();//az összes cella olvasása
            var k = m[24, 56];//egy cella olvasása
                              //legyen a labirintus közepe üres:
            for (int x = 16; x < 48; x++)
                for (int y = 16; y < 48; y++)
                    m[x, y] = new Cell(false, false, false, false);//egy cella írása
            m.Solve(); //külön meg kell hívni
                       //általános rajzoló metódus:
#if UNITY_EDITOR
            m.Draw((x1, y1, x2, y2) =>
            {
                //ide olyan kód megy amely falat épít 
                //a világban Vector3(x1,fal_magassag,y1)
                //és Vector3(x2, fal_magassag, y2) között
            }/*(itt még vannak paraméterek)*/);
#endif
            m.SaveToFile("cells.txt");//az egész labirintus (bővítésekkel együtt) fájlba mentése
                                      //ha ezt használni szeretnéd, az cellastruktúra összes osztályára kell [Serializable]
#if System_Drawing
            m.DrawToFile("cells.bmp");
#endif
            CellMaze loaded = CellMaze.LoadFromFile("saved.txt") as CellMaze; //a fájl betöltése
#if System_Drawing
            loaded.DrawToFile("cells_copy.bmp");//ugyan az mint output.bmp
#endif
            /////////////////////////////////////////////////////////////////////////////////////////////////
            //kocka labirintus:
            BlockMaze b = new BlockMaze(64, 64);
            b.Generate();
            //a kijáratokat még nem csináltam meg, neked kell:
            b[0, 0] = false;
            b[63, 63] = false;
#if System_Drawing
            b.DrawToFile("blocks.bmp", 1024, 1024);//ha a kép nem elég nagy (minimum 16 * méret) pixelhibák lehetnek 
                                                   //(mert keretet is rajzol a kockáknak)
#endif
#if UNITY_EDITOR
            var labirintus_szelesseg = 100f; //világ koordinátákban
            var labirintus_hosszusag = 100f;
            b.Draw((x, y) =>
            {
                //ide olyan kód megy amely kockát épít 
                //a világban, melynek középpontja:
                //Vector3(x, kocka_elhossz/2 ,y)
                //a kocka élhossza: labirintus_szelesseg/b.Width
            }, labirintus_szelesseg, labirintus_hosszusag);
#endif
            b[33, 33] = new Block(false, true); // a 33,33-as kocka legyen kulcs
            b.SaveToTxtFile<Block>("blocks.txt"); //a labirintus szövegfájlba mentése
                                                  //ha saját struktúrát használsz mentéshez akkor meg kell adnod a típusát paraméterként <Block>
            var c = BlockMaze.LoadFromTxtFile<Block>("blocks.txt"); //a labirintus szövegfájlból betöltése
#warning //teszteltem a githubos labirintusfájlokkal
            var kulcs = (c[33, 33] as Block).k.vane; //igaznak kéne lennie
#if System_Drawing
            c.DrawToFile("blocks_copy.bmp", 1024, 1024);
#endif
        }
    }
}
#endif
