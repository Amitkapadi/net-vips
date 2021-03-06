﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace NetVips.Tests
{
    public class ArithmeticTests : IClassFixture<TestsFixture>
    {
        private Image _image;
        private Image _colour;
        private Image _mono;
        private Image[] _allImages;

        public ArithmeticTests()
        {
            _image = Image.MaskIdeal(100, 100, 0.5, reject: true, optical: true);
            _colour = _image * new[] {1, 2, 3} + new[] {2, 3, 4};
            _mono = _colour[1];
            _allImages = new[]
            {
                _mono,
                _colour
            };
        }

        #region helpers

        internal void RunArith(Func<object, object, object> func, string[] formats = null)
        {
            if (formats == null)
            {
                formats = Helper.AllFormats;
            }

            foreach (var x in _allImages)
            {
                foreach (var y in formats)
                {
                    foreach (var z in formats)
                    {
                        Helper.RunImage2(x.Cast(y), x.Cast(z), func);
                    }
                }
            }
        }

        internal void RunArithConst(Func<object, object, object> func, string[] formats = null)
        {
            if (formats == null)
            {
                formats = Helper.AllFormats;
            }

            foreach (var x in _allImages)
            {
                foreach (var y in formats)
                {
                    Helper.RunConst(func, x.Cast(y), (double) 2);
                }
            }

            foreach (var y in formats)
            {
                Helper.RunConst(func, _colour.Cast(y), new[] {1, 2, 3});
            }
        }

        /// <summary>
        /// run a function on an image,
        /// 50,50 and 10,10 should have different values on the test image
        /// </summary>
        /// <param name="im"></param>
        /// <param name="func"></param>
        /// <returns>None</returns>
        internal void RunImageunary(Image im, Func<object, object> func)
        {
            Helper.RunCmp(im, 50, 50, x => Helper.RunFn(func, x));
            Helper.RunCmp(im, 10, 10, x => Helper.RunFn(func, x));
        }

        internal void RunUnary(IEnumerable<Image> images, Func<object, object> func, string[] formats = null)
        {
            if (formats == null)
            {
                formats = Helper.AllFormats;
            }

            foreach (var x in images)
            {
                foreach (var y in formats)
                {
                    RunImageunary(x.Cast(y), func);
                }
            }
        }

        #endregion

        #region test overloadable operators

        [Fact]
        public void TestAdd()
        {
            dynamic Add(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage + x;
                }

                return x + y;
            }

            RunArithConst(Add);
            RunArith(Add);
        }

        [Fact]
        public void TestSub()
        {
            dynamic Sub(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    // There's no __rsub__ equivalent in C# :(
                    return Operation.Call("linear", null, rightImage, -1, x) as Image;
                }

                return x - y;
            }

            RunArithConst(Sub);
            RunArith(Sub);
        }

        [Fact]
        public void TestMul()
        {
            dynamic Mul(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage * x;
                }

                return x * y;
            }

            RunArithConst(Mul);
            RunArith(Mul);
        }

        [Fact]
        public void TestDiv()
        {
            dynamic Div(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    // There's no  __rdiv__ & __pow__ equivalent in C# :(
                    return (Image.CallEnum(rightImage, -1, "math2", "pow") as Image) * x;
                }

                return x / y;
            }

            // (const / image) needs (image ** -1), which won't work for complex
            RunArithConst(Div, Helper.NonComplexFormats);
            RunArith(Div);
        }

        [Fact]
        public void TestFloorDiv()
        {
            dynamic FloorDiv(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    // There's no  __rfloordiv__ & __pow__ equivalent in C# :(
                    return ((Image.CallEnum(rightImage, -1, "math2", "pow") as Image) * x as Image)?.Floor();
                }

                if (x is Image leftImage)
                {
                    // There's no  __floordiv__ equivalent in C# :(
                    return (leftImage / y as Image)?.Floor();
                }

                return Math.Floor(x / y);
            }

            // (const // image) needs (image ** -1), which won't work for complex
            RunArithConst(FloorDiv, Helper.NonComplexFormats);
            RunArith(FloorDiv, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestPow()
        {
            dynamic Pow(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    // There's no  __rpow__ equivalent in C# :(
                    return Image.CallEnum(rightImage, x, "math2", "wop") as Image;
                }

                if (x is Image leftImage)
                {
                    // There's no  __pow__ equivalent in C# :(
                    return Image.CallEnum(leftImage, y, "math2", "pow") as Image;
                }

                return Math.Pow(x, y);
            }

            // (image ** x) won't work for complex images ... just test non-complex
            RunArithConst(Pow, Helper.NonComplexFormats);
            RunArith(Pow, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestAnd()
        {
            dynamic And(dynamic x, dynamic y)
            {
                // C# doesn't allow bools on doubles
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                if (y is double dblY)
                {
                    y = (int) dblY;
                }

                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage & x;
                }

                return x & y;
            }

            RunArithConst(And, Helper.NonComplexFormats);
            RunArith(And, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestOr()
        {
            dynamic Or(dynamic x, dynamic y)
            {
                // C# doesn't allow bools on doubles
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                if (y is double dblY)
                {
                    y = (int) dblY;
                }

                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage | x;
                }

                return x | y;
            }

            RunArithConst(Or, Helper.NonComplexFormats);
            RunArith(Or, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestXor()
        {
            dynamic Xor(dynamic x, dynamic y)
            {
                // C# doesn't allow bools on doubles
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                if (y is double dblY)
                {
                    y = (int) dblY;
                }

                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage ^ x;
                }

                return x ^ y;
            }

            RunArithConst(Xor, Helper.NonComplexFormats);
            RunArith(Xor, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestMore()
        {
            dynamic More(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage < x;
                }

                if (y is Image || x is Image)
                {
                    return x > y;
                }

                return x > y ? 255 : 0;
            }

            RunArithConst(More);
            RunArith(More);
        }

        [Fact]
        public void TestMoreEq()
        {
            dynamic MoreEq(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage <= x;
                }

                if (y is Image || x is Image)
                {
                    return x >= y;
                }

                return x >= y ? 255 : 0;
            }

            RunArithConst(MoreEq);
            RunArith(MoreEq);
        }

        [Fact]
        public void TestLess()
        {
            dynamic Less(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage > x;
                }

                if (y is Image || x is Image)
                {
                    return x < y;
                }

                return x < y ? 255 : 0;
            }

            RunArithConst(Less);
            RunArith(Less);
        }

        [Fact]
        public void TestLessEq()
        {
            dynamic LessEq(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage >= x;
                }

                if (y is Image || x is Image)
                {
                    return x <= y;
                }

                return x <= y ? 255 : 0;
            }

            RunArithConst(LessEq);
            RunArith(LessEq);
        }

        [Fact]
        public void TestEqual()
        {
            dynamic Equal(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage == x;
                }

                if (y is Image || x is Image)
                {
                    return x == y;
                }

                return x == y ? 255 : 0;
            }

            RunArithConst(Equal);
            RunArith(Equal);
        }

        [Fact]
        public void TestNotEq()
        {
            dynamic NotEq(dynamic x, dynamic y)
            {
                if (y is Image rightImage && !(x is Image))
                {
                    return rightImage != x;
                }

                if (y is Image || x is Image)
                {
                    return x != y;
                }

                return x != y ? 255 : 0;
            }

            RunArithConst(NotEq);
            RunArith(NotEq);
        }

        [Fact]
        public void TestAbs()
        {
            dynamic Abs(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Abs();
                }

                return Math.Abs(x);
            }

            var im = _colour * -1;
            RunUnary(new[]
            {
                im
            }, Abs);
        }

        [Fact]
        public void TestLShift()
        {
            dynamic LShift(dynamic x)
            {
                // C# doesn't allow bools on doubles
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                return x << 2;
            }

            // we don't support constant << image, treat as a unary
            RunUnary(_allImages, LShift, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestRShift()
        {
            dynamic RShift(dynamic x)
            {
                // C# doesn't allow bools on doubles
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                return x >> 2;
            }

            // we don't support constant >> image, treat as a unary
            RunUnary(_allImages, RShift, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestMod()
        {
            dynamic Mod(dynamic x)
            {
                return x % 2;
            }

            // we don't support constant % image, treat as a unary
            RunUnary(_allImages, Mod, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestPos()
        {
            dynamic Pos(dynamic x)
            {
                return x;
            }

            RunUnary(_allImages, Pos);
        }

        [Fact]
        public void TestNeg()
        {
            dynamic Neg(dynamic x)
            {
                return x * -1;
            }

            RunUnary(_allImages, Neg);
        }

        [Fact]
        public void TestInvert()
        {
            dynamic Invert(dynamic x)
            {
                if (x is double dblX)
                {
                    x = (int) dblX;
                }

                return (x ^ -1) & 0xff;
            }

            // image ^ -1 is trimmed to image max so it's hard to test for all formats
            // just test uchar
            RunUnary(_allImages, Invert, new[] {Enums.BandFormat.Uchar});
        }

        #endregion

        #region test the rest of VipsArithmetic

        [Fact]
        public void TestAvg()
        {
            var im = Image.Black(50, 100);
            var test = im.Insert(im + 100, 50, 0, expand: true);

            foreach (var fmt in Helper.AllFormats)
            {
                Assert.Equal(50, test.Cast(fmt).Avg());
            }
        }

        [Fact]
        public void TestDeviate()
        {
            var im = Image.Black(50, 100);
            var test = im.Insert(im + 100, 50, 0, expand: true);

            foreach (var fmt in Helper.NonComplexFormats)
            {
                Assert.Equal(50, test.Cast(fmt).Deviate(), 2);
            }
        }

        [Fact]
        public void TestPolar()
        {
            var im = Image.Black(100, 100) + 100;
            im = im.Complexform(im);

            im = im.Polar();

            Assert.Equal(100 * Math.Pow(2, 0.5), im.Real().Avg(), 4);
            Assert.Equal(45, im.Imag().Avg());
        }

        [Fact]
        public void TestRect()
        {
            var im = Image.Black(100, 100);
            im = (im + 100 * Math.Pow(2, 0.5)).Complexform(im + 45);
            im = im.Rect();

            Assert.Equal(100, im.Real().Avg());
            Assert.Equal(100, im.Imag().Avg());
        }

        [Fact]
        public void TestConjugate()
        {
            var im = Image.Black(100, 100) + 100;
            im = im.Complexform(im);

            im = im.Conj();

            Assert.Equal(100, im.Real().Avg());
            Assert.Equal(-100, im.Imag().Avg());
        }

        [Fact]
        public void TestHistFind()
        {
            var im = Image.Black(50, 100);
            var test = im.Insert(im + 10, 50, 0, expand: true);

            foreach (var fmt in Helper.AllFormats)
            {
                var hist = test.Cast(fmt).HistFind();
                Assert.Equal(new double[] {5000}, hist.Getpoint(0, 0));
                Assert.Equal(new double[] {5000}, hist.Getpoint(10, 0));
                Assert.Equal(new double[] {0}, hist.Getpoint(5, 0));
            }

            test = test * new[] {1, 2, 3};
            foreach (var fmt in Helper.AllFormats)
            {
                var hist = test.Cast(fmt).HistFind(band: 0);
                Assert.Equal(new double[] {5000}, hist.Getpoint(0, 0));
                Assert.Equal(new double[] {5000}, hist.Getpoint(10, 0));
                Assert.Equal(new double[] {0}, hist.Getpoint(5, 0));

                hist = test.Cast(fmt).HistFind(band: 1);
                Assert.Equal(new double[] {5000}, hist.Getpoint(0, 0));
                Assert.Equal(new double[] {5000}, hist.Getpoint(20, 0));
                Assert.Equal(new double[] {0}, hist.Getpoint(5, 0));
            }
        }

        [Fact]
        public void TestHistFindIndexed()
        {
            var im = Image.Black(50, 100);
            var test = im.Insert(im + 10, 50, 0, expand: true);

            // There's no  __floordiv__ equivalent in C# :(
            var index = (test / 10).Floor();

            foreach (var x in Helper.NonComplexFormats)
            {
                foreach (var y in new[] {Enums.BandFormat.Uchar, Enums.BandFormat.Ushort})
                {
                    var a = test.Cast(x);
                    var b = index.Cast(y);
                    var hist = a.HistFindIndexed(b);
                    Assert.Equal(new double[] {0}, hist.Getpoint(0, 0));
                    Assert.Equal(new double[] {50000}, hist.Getpoint(1, 0));
                }
            }
        }

        [Fact]
        public void TestHistFindNdim()
        {
            var im = Image.Black(100, 100) + new[] {1, 2, 3};

            foreach (var fmt in Helper.NonComplexFormats)
            {
                var hist = im.Cast(fmt).HistFindNdim();

                Assert.Equal(10000, hist.Getpoint(0, 0)[0]);
                Assert.Equal(0, hist.Getpoint(5, 5)[5]);

                hist = im.Cast(fmt).HistFindNdim(bins: 1);

                Assert.Equal(10000, hist.Getpoint(0, 0)[0]);
                Assert.Equal(1, hist.Width);
                Assert.Equal(1, hist.Height);
                Assert.Equal(1, hist.Bands);
            }
        }

        [Fact]
        public void TestHoughCircle()
        {
            var test = Image.Black(100, 100).DrawCircle(new double[] {100}, 50, 50, 40);

            foreach (var fmt in Helper.AllFormats)
            {
                var im = test.Cast(fmt);
                var hough = im.HoughCircle(minRadius: 35, maxRadius: 45);

                var maxPos = hough.MaxPos();
                var v = maxPos[0];
                var x = (int) maxPos[1];
                var y = (int) maxPos[2];

                var vec = hough.Getpoint(x, y);
                var r = Array.IndexOf(vec, vec.Min(d => v)) + 35;

                Assert.Equal(50, x);
                Assert.Equal(50, y);
                Assert.Equal(40, r);
            }
        }

        [SkippableFact]
        public void TestHoughLine()
        {
            // hough_line changed the way it codes parameter space in 8.7 ... don't
            // test earlier versions
            Skip.IfNot(Base.AtLeastLibvips(8, 7), "requires libvips >= 8.7");

            var test = Image.Black(100, 100).DrawLine(new double[] {100}, 10, 90, 90, 10);

            foreach (var fmt in Helper.AllFormats)
            {
                var im = test.Cast(fmt);
                var hough = im.HoughLine();

                var maxPos = hough.MaxPos();
                var x = maxPos[1];
                var y = maxPos[2];

                var angle = Math.Floor(180.0 * x / hough.Width);
                var distance = Math.Floor(test.Height * y / hough.Height);

                Assert.Equal(45, angle);
                Assert.Equal(70, distance);
            }
        }

        [Fact]
        public void TestSin()
        {
            dynamic Sin(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Sin();
                }

                return Math.Sin(Math.PI / 180 * x);
            }

            RunUnary(_allImages, Sin, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestCos()
        {
            dynamic Cos(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Cos();
                }

                return Math.Cos(Math.PI / 180 * x);
            }

            RunUnary(_allImages, Cos, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestTan()
        {
            dynamic Tan(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Tan();
                }

                return Math.Tan(Math.PI / 180 * x);
            }

            RunUnary(_allImages, Tan, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestASin()
        {
            dynamic ASin(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Asin();
                }

                return Math.Asin(x) * (180.0 / Math.PI);
            }

            var im = (Image.Black(100, 100) + new[] {1, 2, 3}) / 3.0;
            RunUnary(new[] {im}, ASin, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestACos()
        {
            dynamic ACos(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Acos();
                }

                return Math.Acos(x) * (180.0 / Math.PI);
            }

            var im = (Image.Black(100, 100) + new[] {1, 2, 3}) / 3.0;
            RunUnary(new[] {im}, ACos, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestATan()
        {
            dynamic ATan(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Atan();
                }

                return Math.Atan(x) * (180.0 / Math.PI);
            }

            var im = (Image.Black(100, 100) + new[] {1, 2, 3}) / 3.0;
            RunUnary(new[] {im}, ATan, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestLog()
        {
            dynamic Log(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Log();
                }

                return Math.Log(x);
            }

            RunUnary(_allImages, Log, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestExp()
        {
            dynamic Exp(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Exp();
                }

                return Math.Exp(x);
            }

            RunUnary(_allImages, Exp, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestExp10()
        {
            dynamic Exp10(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Exp10();
                }

                return Math.Pow(10, x);
            }

            RunUnary(_allImages, Exp10, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestFloor()
        {
            dynamic Floor(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Floor();
                }

                return Math.Floor(x);
            }

            RunUnary(_allImages, Floor, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestCeil()
        {
            dynamic Ceil(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Ceil();
                }

                return Math.Ceiling(x);
            }

            RunUnary(_allImages, Ceil, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestRint()
        {
            dynamic Rint(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Rint();
                }

                return Math.Round(x);
            }

            RunUnary(_allImages, Rint, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestSign()
        {
            dynamic Sign(dynamic x)
            {
                if (x is Image image)
                {
                    return image.Sign();
                }

                if (x > 0)
                {
                    return 1;
                }

                if (x < 0)
                {
                    return -1;
                }

                return 0;
            }

            RunUnary(_allImages, Sign, Helper.NonComplexFormats);
        }

        [Fact]
        public void TestMax()
        {
            var test = Image.Black(100, 100).DrawRect(new double[] {100}, 40, 50, 1, 1);

            foreach (var fmt in Helper.AllFormats)
            {
                var v = test.Cast(fmt).Max();

                Assert.Equal(100, v);

                var maxPos = test.Cast(fmt).MaxPos();
                v = maxPos[0];
                var x = maxPos[1];
                var y = maxPos[2];

                Assert.Equal(100, v);
                Assert.Equal(40, x);
                Assert.Equal(50, y);
            }
        }

        [Fact]
        public void TestMin()
        {
            var test = (Image.Black(100, 100) + 100).DrawRect(new double[] {0}, 40, 50, 1, 1);

            foreach (var fmt in Helper.AllFormats)
            {
                var v = test.Cast(fmt).Min();

                Assert.Equal(0, v);

                var minPos = test.Cast(fmt).MinPos();
                v = minPos[0];
                var x = minPos[1];
                var y = minPos[2];

                Assert.Equal(0, v);
                Assert.Equal(40, x);
                Assert.Equal(50, y);
            }
        }

        [Fact]
        public void TestMeasure()
        {
            var im = Image.Black(50, 50);
            var test = im.Insert(im + 10, 50, 0, expand: true);

            foreach (var fmt in Helper.NonComplexFormats)
            {
                var a = test.Cast(fmt);
                var matrix = a.Measure(2, 1);
                var p1 = matrix.Getpoint(0, 0)[0];
                var p2 = matrix.Getpoint(0, 1)[0];

                Assert.Equal(0, p1);
                Assert.Equal(10, p2);
            }
        }

        [SkippableFact]
        public void TestFindTrim()
        {
            Skip.IfNot(Helper.Have("find_trim"), "no find_trim in this vips, skipping test");

            var im = Image.Black(50, 60) + 100;
            var test = im.Embed(10, 20, 200, 300, extend: "white");

            foreach (var x in Helper.UnsignedFormats.Concat(Helper.FloatFormats).ToArray())
            {
                var a = test.Cast(x);
                var trim = a.FindTrim();
                var left = trim[0];
                var top = trim[1];
                var width = trim[2];
                var height = trim[3];

                Assert.Equal(10, left);
                Assert.Equal(20, top);
                Assert.Equal(50, width);
                Assert.Equal(60, height);
            }

            var testRgb = test.Bandjoin(new[] {test, test});
            var trim2 = testRgb.FindTrim(background: new double[] {255, 255, 255});
            var left2 = trim2[0];
            var top2 = trim2[1];
            var width2 = trim2[2];
            var height2 = trim2[3];

            Assert.Equal(10, left2);
            Assert.Equal(20, top2);
            Assert.Equal(50, width2);
            Assert.Equal(60, height2);
        }

        [Fact]
        public void TestProfile()
        {
            var test = Image.Black(100, 100).DrawRect(new double[] {100}, 40, 50, 1, 1);

            foreach (var fmt in Helper.NonComplexFormats)
            {
                var profile = test.Cast(fmt).Profile();
                var columns = profile[0] as Image;
                var rows = profile[1] as Image;

                var minPos = columns.MinPos();
                var v = minPos[0];
                var x = minPos[1];
                var y = minPos[2];

                Assert.Equal(50, v);
                Assert.Equal(40, x);
                Assert.Equal(0, y);

                minPos = rows.MinPos();
                v = minPos[0];
                x = minPos[1];
                y = minPos[2];

                Assert.Equal(40, v);
                Assert.Equal(0, x);
                Assert.Equal(50, y);
            }
        }

        [Fact]
        public void TestProject()
        {
            var im = Image.Black(50, 50);
            var test = im.Insert(im + 10, 50, 0, expand: true);

            foreach (var fmt in Helper.NonComplexFormats)
            {
                var profile = test.Cast(fmt).Project();
                var columns = profile[0] as Image;
                var rows = profile[1] as Image;

                Assert.Equal(new double[] {0}, columns.Getpoint(10, 0));
                Assert.Equal(new double[] {50 * 10}, columns.Getpoint(70, 0));

                Assert.Equal(new double[] {50 * 10}, rows.Getpoint(0, 10));
            }
        }

        [Fact]
        public void TestStats()
        {
            var im = Image.Black(50, 50);
            var test = im.Insert(im + 10, 50, 0, expand: true);

            foreach (var fmt in Helper.NonComplexFormats)
            {
                var a = test.Cast(fmt);
                var matrix = a.Stats();

                Assert.Equal(new[] {a.Min()}, matrix.Getpoint(0, 0));
                Assert.Equal(new[] {a.Max()}, matrix.Getpoint(1, 0));
                Assert.Equal(new double[] {50 * 50 * 10}, matrix.Getpoint(2, 0));
                Assert.Equal(new double[] {50 * 50 * 100}, matrix.Getpoint(3, 0));
                Assert.Equal(new[] {a.Avg()}, matrix.Getpoint(4, 0));
                Assert.Equal(new[] {a.Deviate()}, matrix.Getpoint(5, 0));

                Assert.Equal(new[] {a.Min()}, matrix.Getpoint(0, 1));
                Assert.Equal(new[] {a.Max()}, matrix.Getpoint(1, 1));
                Assert.Equal(new double[] {50 * 50 * 10}, matrix.Getpoint(2, 1));
                Assert.Equal(new double[] {50 * 50 * 100}, matrix.Getpoint(3, 1));
                Assert.Equal(new[] {a.Avg()}, matrix.Getpoint(4, 1));
                Assert.Equal(new[] {a.Deviate()}, matrix.Getpoint(5, 1));
            }
        }

        [Fact]
        public void TestSum()
        {
            foreach (var fmt in Helper.AllFormats)
            {
                var im = Image.Black(50, 50);
                var im2 = Enumerable.Range(0, 100).Where(i => i % 10 == 0).Select(x => (im + x).Cast(fmt)).ToArray();
                var im3 = Image.Sum(im2);

                Assert.Equal(Enumerable.Range(0, 100).Where(i => i % 10 == 0).Sum(), im3.Max());
            }
        }

        #endregion
    }
}