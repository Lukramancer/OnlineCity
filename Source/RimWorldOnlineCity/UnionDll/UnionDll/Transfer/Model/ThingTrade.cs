﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;
using OCUnion;

namespace Model
{
    /// <summary>
    /// Модель хранящая расширенную информацию полученную с игрового объека (при Concrete == true),
    /// либо только некоторую информацию о том, какой объект должен быть (при Concrete == false)
    /// </summary>
    [Serializable]
    public class ThingTrade : ThingEntry
    {
        /// <summary>
        /// Содержиться информация о конкретном объекте
        /// </summary>
        public bool Concrete;

        /// <summary>
        /// Тип вещи
        /// </summary>
        public string DefName { get; set; }
        /// <summary>
        /// Материал из чего изготовлено
        /// </summary>
        public string StuffName { get; set; }
        /// <summary>
        /// Текущая прочность, если 0 считается масксимальной 
        /// Либо минимально требуемая прочность (при Concrete == false)
        /// </summary>
        public int HitPoints { get; set; }
        /// <summary>
        /// Максимальная прочность
        /// Либо всегда 100 (при Concrete == false)
        /// </summary>
        public int MaxHitPoints { get; set; }
        /// <summary>
        /// Качество изготовления
        /// </summary>
        public int Quality { get; set; }
        /// <summary>
        /// Снято с трупа, применимо только к одежде
        /// </summary>
        public bool WornByCorpse { get; set; }

        /// <summary>
        /// У нас этого нет, невозможно продать. Вычисляется функцией ExchengeUtils.ChechToSell
        /// </summary>
        /// 
        [NonSerialized]
        public bool NotTrade;

        [NonSerialized]
        private ThingDef Def_p;
        public ThingDef Def
        {
            get
            {
                if (Def_p == null)
                {
                    Def_p = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), DefName);
                }
                return Def_p;
            }
            set { Def_p = value; }
        }

        [NonSerialized]
        private ThingDef StuffDef_p;
        public ThingDef StuffDef
        {
            get
            {
                if (StuffDef_p == null)
                {
                    StuffDef_p = !string.IsNullOrEmpty(StuffName) ? (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), StuffName) : GenStuff.DefaultStuffFor(Def);
                }
                return StuffDef_p;
            }
            set { StuffDef_p = value; }
        }

        public string LabelText
        {
            get
            {
                return Name + (Count > 1 ? " x" + Count.ToString(): "") + Environment.NewLine
                    + (Concrete
                        ? "Качество {0}. Прочность {1} из {2}".NeedTranslate(((QualityCategory)Quality).GetLabel(), HitPoints, MaxHitPoints)
                            + (WornByCorpse ? " Снято с трупа".NeedTranslate() : "")
                        : "Качество {0} и лучше. Прочность {1}% и больше".NeedTranslate(((QualityCategory)Quality).GetLabel(), HitPoints)
                            + (WornByCorpse ? " Может быть снято с трупа".NeedTranslate() : "")
                        )
                    ;
            }
        }

        protected ThingTrade()
        { }

        public bool MatchesThing(Thing thing)
        {
            //быстрая проверка
            if (thing == null) return false;
            if (DefName != thing.def.defName) return false;

            var testThing = CreateTrade(thing, 1);

            //общая проверка
            if (testThing.StuffName != StuffName
                || testThing.Quality < Quality
                || testThing.WornByCorpse && !WornByCorpse
                ) return false;

            //в зависимости от Concrete
            int hitPrecent = (testThing.HitPoints + 1) * 100 / testThing.MaxHitPoints;
            if (Concrete)
                return hitPrecent >= HitPoints * 100 / MaxHitPoints;
            else
                return hitPrecent >= HitPoints;

            //todo Проверка схожести средствами игры, для надёжности и идентификации индивидуальности пешек
        }

        public static ThingTrade CreateTrade(Thing thing, int count)
        {
            var that = new ThingTrade();
            that.SetBaseInfo(thing, count);
            that.Concrete = true;

            that.DefName = thing.def.defName;
            that.StuffName = thing.Stuff == null ? null : thing.Stuff.defName;
            that.HitPoints = thing.HitPoints;
            that.MaxHitPoints = thing.MaxHitPoints;
            
            QualityCategory qq;
            if (QualityUtility.TryGetQuality(thing, out qq)) that.Quality = (int)qq;

            Apparel thingA = thing as Apparel;
            if (thingA != null) that.WornByCorpse = thingA.WornByCorpse;

            // Не заполняются:
            //Data

            return that;
        }

        public static ThingTrade CreateTrade(ThingDef thingDef, float minHitPointsPercents, QualityCategory minQualities, int count)
        {
            var that = new ThingTrade();
            that.Name = thingDef.LabelCap;
            that.Count = count;

            that.DefName = thingDef.defName;
            that.HitPoints = (int)(minHitPointsPercents * 100);
            that.MaxHitPoints = 100;

            that.Quality = (int)minQualities;

            // Не заполняются:
            //Data
            //OriginalID
            //StuffName
            //WornByCorpse

            return that;
        }

    }
}
