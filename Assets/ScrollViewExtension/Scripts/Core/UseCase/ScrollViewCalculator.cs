using System;
using ScrollViewExtension.Scripts.Adapter.DTO;
using ScrollViewExtension.Scripts.Common;
using ScrollViewExtension.Scripts.Core.Entity.Interface;
using ScrollViewExtension.Scripts.Core.Service.Interface;
using ScrollViewExtension.Scripts.Core.UseCase.Interface;
using UnityEngine;

namespace ScrollViewExtension.Scripts.Core.UseCase
{
    internal class ScrollViewCalculator<TData> : UseCaseBase<TData>, IScrollViewCalculator where TData : ScrollItemBase, new()
    {
        private const float Epsilon = Const.Epsilon;
        
        private static ScrollViewCalculator<TData> instance;
        
        private float maxOffset;
        private float lastOffsetLength;
        private float lastContentPos;

        public static ScrollViewCalculator<TData> CreateInstance(IScrollViewEntity<TData> viewEntity, IFindIndex<TData> findIndex)
        {
            return instance ??= new ScrollViewCalculator<TData>(viewEntity, findIndex);
        }

        public int CalculateInstanceNumber(bool needDoubleGen)
        {
            var number = Mathf.CeilToInt(viewEntity.GetViewLength / viewEntity.GetItemMinLength());
            
            number += needDoubleGen ? 0 : 1; // ジャストサイズを防ぐために+1
            number *= needDoubleGen ? 2 : 1;

            return viewEntity.Data.Count >= number ? number : viewEntity.Data.Count;
        }

        public Vector2 CalculateContentSize()
        {
            if (viewEntity.Data.Count <= 0 || viewEntity.Data is null) return Vector2.zero;
            
            var size = viewEntity.IsVertical
                ? new Vector2(viewEntity.ContentSize.x, viewEntity.GetContentLength(viewEntity.Data.Count))
                : new Vector2(viewEntity.GetContentLength(viewEntity.Data.Count), viewEntity.ContentSize.y);

            return size;
        }

        public float CalculateBarPosition(int index)
        {
            var vertical = Mathf.Clamp01(1 - viewEntity.GetContentLength(index) / GetScrollableRange());
         
            if(viewEntity.IsVertical) 
                return vertical;
            
            return  1 - vertical;
        }

        /// <summary>
        /// 現在のパディングと新しいパディングを元にローリングの計算を行う関数です。
        /// </summary>
        /// <param name="currentPadding">現在のパディング。</param>
        /// <param name="newPadding">新しいパディング。</param>
        /// <param name="startIndex">開始のインデックス。</param>
        /// <returns>計算されたローリングの値を返します。</returns>
        public int CalculateRolling(Vector4 currentPadding, Vector4 newPadding, int startIndex)
        {
            if (currentPadding.LessThan(Vector4.zero) || newPadding.LessThan(Vector4.zero))
                throw new ArgumentException("padding can not be negative");
            
            // パディングの新旧の点を決定する
            var newP = viewEntity.IsVertical ? newPadding.x : newPadding.z;
            var currentP = viewEntity.IsVertical ? currentPadding.x : currentPadding.z;

            var rolling = 0; // ローリングの値を初期化
            var diff = newP - currentP; // パディングの差分を計算
            
            // パディングの差分を元にローリングの値を計算
            switch (diff)
            {
                case > Epsilon:
                {
                    // 下方向へのローリングを計算
                    while (diff > Epsilon)
                    {
                        rolling++;
                        // アイテムのサイズを計算に反映
                        diff -= viewEntity.GetItemSize(startIndex);
                        startIndex++;
                    }

                    break;
                }
                case < -Epsilon:
                {
                    // 上方向へのローリングを計算
                    while (diff < -Epsilon)
                    {
                        rolling--;
                        // アイテムのサイズを計算に反映
                        diff += viewEntity.GetItemSize(startIndex - 1);
                        startIndex--;
                    }

                    break;
                }
            }

            return rolling;
        }

        /// <summary>
        /// フレームワーク内の特定の位置に基づいてOffsetを計算します。
        /// </summary>
        /// <param name="count">対象となる項目の数</param>
        /// <param name="contentPos">コンテンツ位置。XおよびY座標で指定します。</param>
        /// <param name="preload"></param>
        /// <returns>計算されたオフセットを表す4次元ベクトル</returns>
        /// <exception cref="ArgumentException">countが0の場合にスローされます</exception>
        public Vector4 CalculateOffset(int count, Vector3 contentPos, bool preload)
        {
            // カウントが0であることは許可されていません
            if (count <= 0)
                throw new ArgumentException("count can not smaller than 0");

            // コンテンツの長さを計算します
            var contentLength = viewEntity.GetContentLength(viewEntity.Data.Count);

            // 最大オフセットを更新します
            maxOffset = contentLength - viewEntity.GetContentLength(count, viewEntity.Data[^count].Index);

            // コンテンツの位置が負の場合は、0にリセットします
            contentPos = Mathf.Min(-contentPos.x, contentPos.y) < 0 ? Vector3.zero : contentPos;

            // コンテンツの位置がコンテンツの長さを超える場合、最大オフセットにリセットします
            contentPos = Mathf.Max(-contentPos.x, contentPos.y) > contentLength
                ? (viewEntity.IsVertical ? maxOffset : -maxOffset) * Vector3.one
                : contentPos;

            var offset = Vector3.zero;
            
            if (preload)
            {
                var length = CalculateBarOffset(viewEntity.Data[^count].Index, count, contentPos);
                offset = new Vector3(-length, length, 0);
            }
            
            // オフセットを計算します
            var target = viewEntity.GetContentLength(findIndex.ByPosition(contentPos - offset, viewEntity), 0, false);
            
            // オフセットを含むベクトルを生成して返します
            return CreateVectorWithOffset(viewEntity.IsVertical,
                Mathf.Clamp(target, 0, maxOffset));
        }

        public void Dispose()
        {
            instance = null;
        }

        private float CalculateBarOffset(int index, int count, Vector3 conPos)
        {
            var length = viewEntity.GetContentLength(count, index) * 0.25f; // 生成数が2倍なので、四等分にします

            var cP = viewEntity.IsVertical ? conPos.y : -conPos.x;
            var isSameDir = (cP > lastContentPos && length > lastOffsetLength) ||
                            (cP < lastContentPos && length < lastOffsetLength);

            if (isSameDir) // 同じ方向の場合は長さを固定する必要がある、じゃないとぶれが発生します
            {
                length = lastOffsetLength;
            }

            lastOffsetLength = length;
            lastContentPos = cP;
            
            return length;
        }

        private Vector4 CreateVectorWithOffset(bool isVertical, float offset)
        {
            return isVertical
                ? new Vector4(offset + viewEntity.DefaultPadding.top, 0, viewEntity.DefaultPadding.left, viewEntity.DefaultPadding.right)
                : new Vector4(viewEntity.DefaultPadding.top, viewEntity.DefaultPadding.bottom, offset + viewEntity.DefaultPadding.left, 0);
        }

        /// <summary>
        /// 移動できる長さ
        /// </summary>
        /// <returns></returns>
        private float GetScrollableRange()
        {
            return viewEntity.GetContentLength(viewEntity.Data.Count) - viewEntity.GetViewLength;
        }

        private ScrollViewCalculator(IScrollViewEntity<TData> viewEntity, IFindIndex<TData> findIndex) : base(viewEntity, findIndex)
        {
        }
    }
}