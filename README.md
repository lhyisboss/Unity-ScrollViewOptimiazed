# Unity-ScrollVeiwExtension
A simple extension based on VerticalOrHorizontalLayoutGroup and GridLayoutGroup.

## Verticalとhorizontal方向のDemo
![Alt text for the GIF](/Img/vertical.gif)  

![Alt text for the GIF](/Img/horizontal.gif)  

demoは501個のデータを表示する例です。  
index,bar位置を指定して表示することもできます。  
途中サイズ変更も可能です。

## 説明
verticalやhorizontal layout groupを経由で生成数を最適化の手段の一つです。

現在何ができる：  
①最小生成数でスクロールの内容を表示していきます。  
②途中でサイズ変更は可能です。  
③indexを指定して表示することが可能です  
④barの位置を指定して表示することが可能です。  

プロジェクトの構成：  
<img src="./Img/Unity-ScrollViewOptimized.png" width="50%" height="50%">

フォルダの構成から見て分かると思いますが、典型的なClean Architectureです。

※：  
自分は最近あったケースは以上の機能で大体足りますが、もし他に何か入って欲しい機能があったら、全然追加しますので、是非知らせください。  
GridLayout経由の拡張も今作成中で、次のバージョンでまとめてアップします。

## 余談
自分は今Unity側の仕事探しています。もし、貴社ちょうど今人手足りないなら、是非検討をお願いします。  
性格としては穏やかで、ゲーム作りに熱心です。
