using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace TenhouPointCalculatorBeta3
{
    static class AgareRefactor
    {
        private static int _flagUp;
        private static int _flagDown;
        private static Player _upPlayer;
        private static Player _downPlayer;
        private static IEnumerable<Player> _downPlayers;
        private static FuFanPoint _targetPoint;
        private static int? _upPoint;
        private static int? _downOyaPoint;
        private static int? _downKoPoint;
        private static bool _isOyaAgare;
        private static bool _isTsumo;
        private static string _situation;
        //↓为双响所用数据，不被初始化
        public static int BenChangTemp;
        public static bool IsOyaAgareFirst;



        public static void Method(int flagUp, int flagDown)
        {
            #region 初始化
            _flagUp = flagUp;
            _flagDown = flagDown;
            _upPlayer = null;
            _downPlayer = null;
            _downPlayers = null;
            _targetPoint = null;
            _upPoint = null;
            _downOyaPoint = null;
            _downKoPoint = null;
            _isOyaAgare = false;
            _isTsumo = false;
            #endregion

            //对号入座
            _upPlayer = Element.Players[_flagUp - 1];
            _downPlayer = Element.Players[_flagDown - 1];
            _downPlayers = Element.Players.Where(p => p.Name != _upPlayer.Name).Select(p => p);
            MainActivity.RunningOtherProgram = true;
            //判断是否亲和牌
            _isOyaAgare = Element.Players[_flagUp - 1].Name == Element.Session.OyaName;
            //判断是否自摸
            _isTsumo = _flagUp == _flagDown;
            //标准化点数
            StandardizePoint();
            if (_targetPoint == null)
            {
                MessageBox.Show("输入点数出错");
                UpdateText.Set(MainActivity.ControlTextView, "谁胡牌？");
            }
            else
            {
                AgareMethod(); //进行点数交换
                if (MainActivity.DoubleRonCheckBox.Checked)
                    DoubleRonPrepare(); //双响准备
                else
                    AfterAgare(); //胡牌后处理
            }
        }

        private static void StandardizePoint()//标准化点数
        {
            var txt = MainActivity.InpuTextView.Text;
            var txtStrings = txt.Split(new[] { '/' });
            int totalPoint;

            if (txt == "") { _targetPoint = null; return; }

            #region "7900"
            // "7900"                   length==1
            if (txtStrings.Length == 1)
            {
                totalPoint = Convert.ToInt32(txt);

                if (_isOyaAgare && _isTsumo)
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.OyaTsumoTotalPoint == totalPoint)
                        .Select(p => p)
                        .FirstOrDefault();
                    _upPoint = _targetPoint?.OyaTsumoTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = _targetPoint?.OyaTsumoLostPoint;
                }
                else if (_isOyaAgare)
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.OyaAgareTotalPoint == totalPoint)
                        .Select(p => p)
                        .FirstOrDefault();
                    _upPoint = _targetPoint?.OyaAgareTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = null;
                }
                else if (_isTsumo)
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p => p.KoTsumoTotalPoint == totalPoint)
                        .Select(p => p)
                        .FirstOrDefault();
                    _upPoint = _targetPoint?.KoTsumoTotalPoint;
                    _downOyaPoint = _targetPoint?.OyaTsumoLostPoint;
                    _downKoPoint = _targetPoint?.KoTsumoLostPoint;
                }
                else
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.KoAgareTotalPoint == totalPoint)
                        .Select(p => p)
                        .FirstOrDefault();
                    _upPoint = _targetPoint?.KoAgareTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = null;
                }
            }
            #endregion

            #region "2000/3900"
            // "2000/3900" 子自摸only   length==2
            else if (txtStrings.Length == 2 && txtStrings[0] != "" && txtStrings[1] != "")
            {
                var lowPoint = Convert.ToInt32(txtStrings[0]) < Convert.ToInt32(txtStrings[1]) ? txtStrings[0] : txtStrings[1];
                var highPoint = lowPoint == txtStrings[0] ? txtStrings[1] : txtStrings[0];
                _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.KoTsumoLostPoint.ToString() == lowPoint &&
                            p.OyaTsumoLostPoint.ToString() == highPoint)
                            .Select(p => p)
                            .FirstOrDefault();
                if (_isOyaAgare == true) _targetPoint = null;
                _upPoint = _targetPoint?.KoTsumoTotalPoint;
                _downOyaPoint = _targetPoint?.OyaTsumoLostPoint;
                _downKoPoint = _targetPoint?.KoTsumoLostPoint;
            }
            #endregion

            #region "//2000"
            // "//2000"    亲自摸only   length==3 && [0]==null && [1]==null
            else if (txtStrings.Length == 3 && txtStrings[0] == "" && txtStrings[1] == "" && txtStrings[2] != "")
            {
                _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.OyaTsumoLostPoint.ToString() == txtStrings[2] &&
                            (p.OyaTsumoTotalPoint / 3).ToString() == txtStrings[2])
                            .Select(p => p)
                            .FirstOrDefault();
                if (_isOyaAgare == false) _targetPoint = null;
                _upPoint = _targetPoint?.OyaTsumoTotalPoint;
                _downOyaPoint = null;
                _downKoPoint = _targetPoint?.OyaTsumoLostPoint;
            }
            #endregion

            #region "30//4" 
            // "30//4"                  length==3 && [0]!=null && [1]==null
            else if (txtStrings.Length == 3 && txtStrings[0] != "" && txtStrings[1] == "" && txtStrings[2] != "" && Convert.ToInt32(txtStrings[0]) <= 110)
            {
                int fan = Convert.ToInt32(txtStrings[2]) < 13 ? Convert.ToInt32(txtStrings[2]) : 13;
                if (fan <= 4)//4翻及以下
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p =>
                            p.Fu.ToString() == txtStrings[0] &&
                            p.Fan == fan)
                        .Select(p => p)
                        .FirstOrDefault();
                }
                else//5翻以上
                {
                    _targetPoint = Element.FuFanPoints.Where(
                        p => p.Fan == fan)
                        .Select(p => p)
                        .FirstOrDefault();
                }
                if (_targetPoint == null) return;
                if (_isOyaAgare && _isTsumo)
                {
                    _upPoint = _targetPoint.OyaTsumoTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = _targetPoint.OyaTsumoLostPoint;
                    if (_targetPoint.Fu != 110 && _targetPoint.Fan != 1)
                        MessageBox.Show("亲家自摸" + _targetPoint.Fu + "符" + _targetPoint.Fan + "番为\n" + _targetPoint.OyaTsumoLostPoint + "点all");
                    else
                        _targetPoint = null;
                }
                else if (_isOyaAgare)
                {
                    _upPoint = _targetPoint.OyaAgareTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = null;
                    if (_targetPoint.Fu != 20)
                        MessageBox.Show("亲家荣和" + _targetPoint.Fu + "符" + _targetPoint.Fan + "番为\n" + _targetPoint.OyaAgareTotalPoint + "点");
                    else
                    {
                        _targetPoint = null;
                    }
                }
                else if (_isTsumo)
                {
                    _upPoint = _targetPoint.KoTsumoTotalPoint;
                    _downOyaPoint = _targetPoint.OyaTsumoLostPoint;
                    _downKoPoint = _targetPoint.KoTsumoLostPoint;
                    if (_targetPoint.Fu != 110 || _targetPoint.Fan != 1)
                        MessageBox.Show("子家自摸" + _targetPoint.Fu + "符" + _targetPoint.Fan + "番为\n" + _targetPoint.KoTsumoLostPoint + "点/" + _targetPoint.OyaTsumoLostPoint + "点");
                    else
                        _targetPoint = null;
                }
                else
                {
                    _upPoint = _targetPoint?.KoAgareTotalPoint;
                    _downOyaPoint = null;
                    _downKoPoint = null;
                    if (_targetPoint.Fu != 20)
                        MessageBox.Show("子家荣和" + _targetPoint.Fu + "符" + _targetPoint.Fan + "番为\n" + _targetPoint.KoAgareTotalPoint + "点");
                    else
                    {
                        _targetPoint = null;
                    }
                }
            }
            #endregion

        }

        private static void AgareMethod()//胡牌算法
        {
            int cb = Element.Session.BenChang;//场棒
            int qb = Element.Session.QianBang;//千棒

            _upPlayer.Point += Convert.ToInt32(_upPoint) + cb * 300 + qb * 1000;

            if (!_isTsumo)//荣和
            {
                _downPlayer.Point -= Convert.ToInt32(_upPoint) + cb * 300;
                _situation = Element.Session.ToString() + Element.Session.BenChang + "本 " + _downPlayer.RealName + " 铳 " + _upPlayer.RealName + " " + _upPoint + "点";
            }
            else//自摸
            {
                foreach (var p in _downPlayers)
                {
                    if (p.Name == Element.Session.OyaName)
                        p.Point -= Convert.ToInt32(_downOyaPoint) + cb * 100;
                    else
                        p.Point -= Convert.ToInt32(_downKoPoint) + cb * 100;
                }
                _situation = Element.Session.ToString() + Element.Session.BenChang + "本 " + _upPlayer.RealName + " 自摸 " + _upPoint + "点";
            }
        }

        private static void DoubleRonPrepare()//双响准备
        {
            UpdateText.Set(MainActivity.DoubleRonCheckBox, false);
            if (BenChangTemp == 0)//三响的时候不交换
                BenChangTemp = Element.Session.BenChang;
            Element.Session.BenChang = 0;
            Element.Session.QianBang = 0;
            IsOyaAgareFirst = _isOyaAgare || IsOyaAgareFirst;
            UpdateText.Set(MainActivity.InpuTextView, "");
        }

        private static void AfterAgare()//胡牌后处理
        {
            //双响后交换场棒
            if (BenChangTemp != 0)
            {
                Element.Session.BenChang = BenChangTemp;
            }

            //判断是否亲家和牌
            MainActivity.IsOyaAgare = _isOyaAgare || IsOyaAgareFirst;
            //判断连庄还是流庄
            if (_isOyaAgare || IsOyaAgareFirst)
            {
                Element.Session.BenChang++;
                foreach (var player in Element.Players)
                {
                    player.IsReach = false;
                }
            }
            else
            {
                Element.Session.BenChang = 0;
                Element.Session.NowSession++;
                //Element.Session.IsNagareMode = true;
                foreach (var player in Element.Players)
                {
                    if (player.Wind == 0)
                        player.Wind = WindEnum.北;
                    else
                        player.Wind--;
                    player.IsReach = false;
                }
            }

            UpdateText.Set(MainActivity.ControlTextView, "(OvO)");
            UpdateText.Set(MainActivity.AgareBtn, "和牌");
            Element.Session.IsAgareMode = false;
            Element.Session.QianBang = 0;
            UpdateText.Set(MainActivity.InpuTextView, "");
            MainActivity.NowSessionNum++;
            Game.Save(_situation);
            End.IsOwari();
            MainActivity.RunningOtherProgram = false;
        }
    }
}