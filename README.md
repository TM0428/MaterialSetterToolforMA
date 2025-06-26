# Material Setter Tool for Modular Avatar

このツールは、色違いのPrefabから自動的にModular Avatar（MA）のMaterial Setterメニューを作成するUnity Editorツールです。

## 概要

アバターの衣装や装飾品などで色違いのバリエーションがある場合、手動でMA Material Setterを設定するのは非常に手間がかかります。このツールを使用することで、色違いのPrefabから自動的にメニュー構造とMaterial Setterを生成できます。

## アクセス方法

Unity Editorのメニューバーから **Tools > Modular Avatar > Material Setter Creator** を選択してツールウィンドウを開きます。

## 機能

- **独立したエディターウィンドウ**: GameObjectにアタッチ不要
- **自動メニュー生成**: アバター配下に指定した名前のオブジェクトを作成
- **MA Menu Installer自動設定**: メニューインストーラーの自動追加と設定
- **MA Menu Item自動生成**: 各色バリエーション用のメニューアイテム作成
- **Material Setter自動設定**: Prefabのマテリアル情報から自動でMaterial Setterを設定
- **動的バリエーション管理**: バリエーションの追加・削除が簡単
- **Undo対応**: 操作の取り消しが可能
- **エラー検証**: 設定漏れや問題を事前にチェック

## 使用方法

### 1. ツールの起動

1. Unity Editorで **Tools > Modular Avatar > Material Setter Creator** を選択
2. Material Setter Creatorウィンドウが開きます

### 2. 基本設定

#### 基本設定
- **アバターのルートオブジェクト**: アバターのルートオブジェクトを設定
- **着せ替え対象のオブジェクト**: 実際に着ている服などの対象オブジェクトを設定

#### メニュー設定
- **メニュー名**: 作成するメニューの名前（デフォルト: "Color"）
- **メニューアイコン**: メニューのアイコン（オプション）

### 3. 色バリエーションの設定

1. **「バリエーション追加」**ボタンで新しい色バリエーションを追加
2. 各バリエーションで以下を設定：
   - **名前**: バリエーションの名前（例：「赤」「青」「緑」）
   - **Prefab**: 色違いのPrefab
   - **アイコン**: メニューアイテムのアイコン（オプション）
3. 不要なバリエーションは**「このバリエーションを削除」**で削除可能

### 4. 実行

1. すべての設定を入力後、エラーがないことを確認
2. **「Material Setterメニューを作成」**ボタンをクリック
3. 自動的にメニュー構造とMaterial Setterが生成される

## 生成される構造

```
Avatar Root
└── [Menu Name] (例: Color)
    ├── MA Menu Installer
    ├── MA Menu Item (SubMenu)
    ├── [Variant 1] (例: 赤)
    │   ├── MA Menu Item (Toggle, Parameter: "ColorSelect", Value: 自動)
    │   └── MA Material Setter
    ├── [Variant 2] (例: 青)
    │   ├── MA Menu Item (Toggle, Parameter: "ColorSelect", Value: 自動)
    │   └── MA Material Setter
    └── [Variant 3] (例: 緑)
        ├── MA Menu Item (Toggle, Parameter: "ColorSelect", Value: 自動)
        └── MA Material Setter
```

## パラメーター設定

- **パラメーター名**: "ColorSelect" (固定)
- **パラメーター値**: 自動割り当て（0, 1, 2...）
- **初期設定**: すべてOFF（どれもデフォルトにしない）
- **Synced**: 有効
- **Saved**: 有効

## 注意事項

### 前提条件
- Modular Avatar v1.8.0以降がインストールされている必要があります
- VRChat SDK3 Avatarsがインストールされている必要があります

### Prefabの構造について
- ツールはPrefab内のRenderer構造を基に、ターゲットオブジェクト内の対応するRendererを探します
- 相対パスでの検索を試行後、名前でのマッチングを行います
- 複雑な階層構造の場合、手動での調整が必要な場合があります

### バリエーション管理
- バリエーションは動的に追加・削除できます
- 最低1つのバリエーションが必要です
- 同名のバリエーションがある場合はスキップされます

## UI機能

### エラー検証
リアルタイムで設定の検証が行われ、以下の状況でエラー・警告が表示されます：
- 必須フィールドの未入力
- Prefabの未設定
- 名前の未入力

### ヘルプ表示
「ヘルプを表示」チェックボックスでヘルプ情報の表示を切り替えできます。

## トラブルシューティング

### よくある問題

1. **マテリアルが正しく設定されない**
   - PrefabとTarget Objectの階層構造が一致しているか確認
   - Renderer名が一致しているか確認

2. **「レンダラーが見つかりません」警告**
   - PrefabとTarget Objectの構造を見直す
   - 必要に応じて手動でMaterial Setterを調整

3. **メニューが表示されない**
   - MA Menu Installerの設定を確認
   - アバターにVRChat Avatar Descriptorが設定されているか確認

### デバッグ情報
ツール実行時にConsoleに詳細な情報が出力されます：
- 作成されたオブジェクト数
- Material Setterの設定状況
- エラーや警告メッセージ

## 更新履歴

### v1.0.0
- エディターウィンドウ形式に変更
- Tools > Modular Avatar メニューに統合
- パラメーター設定を"ColorSelect"固定、値自動、初期設定OFFに変更
- 動的なバリエーション管理機能追加
- UI/UX の大幅改善

## ライセンス

このツールはMITライセンスの下で公開されています。

