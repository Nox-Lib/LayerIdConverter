# LayerIdConverter
![Unityバージョン](https://img.shields.io/badge/Unity-2018.4.23f1-blue) ![.NETバージョン](https://img.shields.io/badge/.NET-4.x-blueviolet) ![ライセンス](https://img.shields.io/github/license/Nox-Lib/LayerIdConverter)

## Overview
複数のプレハブ・シーン内のゲームオブジェクトのレイヤーや、カメラのCullingMaskを一括で変換します。

## Demo
<img src="https://github.com/Nox-Lib/LayerIdConverter/blob/master/Demo/demo1.png" width="440" height="486.5">

## Requirement
- Unity 2018.4.x 以上で動作確認
- .NET 4.x

## Usage
Unityメニューの「Tools > LayerIdConverter」から、「Prefab」もしくは「Scene」を押下します。
<br>
変換を実行すると、変換結果の詳細がログに出力されます。

#### □機能
||説明|
|:---|:---|
|Processing Mode|Normal : 通常モード<br>Camera Only : カメラのみ処理します|
|Match Pattern|パスの一致パターン。正規表現を使用できます。|
|Ignore Pattern|パスの無視パターン。正規表現を使用できます。|
|Layer Convert Patterns|レイヤーIdの変換パターンの設定。<br>右下の+-ボタンよりパターンの追加、削除ができます。<br>パターンを削除したい場合、削除するパターンを選択した状態で-ボタンを押下します。|
|Change Children|ONにすると、子オブジェクトも処理されます。|
|Stop Convert On Error|ONにすると、処理中に致命的なエラーが発生したい場合に処理を中断します。|
|Camera Culling Mask|ONにすると、レイヤーIdの変換パターンに従ってカメラのCullingMaskを変換します。<br>CullingMaskが「Everything」のカメラは処理されません。|
|Leave Old Layer Id|ONにすると、カメラのCullingMaskの変換で変換元のレイヤーを残します。|
|Targets|レイヤーId変換の処理対象のアセット一覧です。<br>一致パターンや無視パターンが指定されていない場合、プロジェクト内の全てのプレハブ（もしくはシーン）が対象となります。|
|Execute|設定された内容に従って処理を実行します。|

#### □レイヤーIdの変換パターンの設定について
レイヤーIdは手動で入力もできますが、「Old Id」もしくは「New Id」の右の「▼」ボタンを押下することで、プロジェクトに設定されているレイヤーの一覧からレイヤーIdを入力できます。

<img src="https://github.com/Nox-Lib/LayerIdConverter/blob/master/Demo/demo2.png" width="286" height="240">

また、以下のようなパターンは実行できません。
- レイヤーIdに0〜31以外が設定されている。
- 「Old Id」と「New Id」に同じレイヤーIdが設定されている。
- 同じレイヤーIdを変換しようとしている。例えば「11 → 12」と「11 → 13」など。

## Licence
[MIT](https://github.com/Nox-Lib/xxx/blob/master/LICENSE)

## Author
[Nox-Lib](https://github.com/Nox-Lib)
