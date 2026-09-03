# Research-06 レポート：個人開発における Unity 2D ゲーム制作と AI 協働の最適解（2026年実績調査）

## 概要

Claude Code（アーキテクト）による開発プロセス自己診断に基づき、個人開発者がAI（Claude / Gemini）とUnity 2Dゲームを制作する上で「実際に機能する最適解」を2024〜2026年の具体例・文献から体系化したレポート。

本プロジェクトが直面した「単体テストは通るが画面は動かない」「参照が壊れる」「ルールが肥大化して無視される」という問題は、AI活用インディー開発における典型的なアンチパターンであり、すでに業界内で確立された解決策が存在する。

---

## Q1：AIにコードを書かせる際、壊れにくくするためのアーキテクチャ設計

### 結論
AIとの協働において最も破綻しにくいのは**「ScriptableObject (SO) アーキテクチャ」**（および Unity 6 の UI Toolkit データバインディング）である。

### なぜ MVC/MVP や Singleton ではなく SO なのか？
* AIは「C#のテキスト空間」の理解は完璧だが、「Unityのシーン階層（ヒエラルキー）」は見えていない（Runtime Blindness）。
* 不足している情報を補うために `transform.Find("Panel/Button")` や `FindFirstObjectByType()` を多用し、シーン構造に密結合した「エディタでしか動かない時限爆弾」を生み出す。

### 解決策：ScriptableObject を「データとイベントの仲介役（バス）」にする
AIには「SOの値を更新するロジック」だけを書かせる。画面との紐付け（インスペクターでのアサイン）は人間が行う。

```csharp
// AIが書くコード（ロジック層）: シーンのUIを知る必要が一切ない
public class PlayerLogic : MonoBehaviour {
    public IntVariableSO playerHP;
    public VoidEventChannelSO onPlayerDied;

    public void TakeDamage(int damage) {
        playerHP.Value -= damage;
        if (playerHP.Value <= 0) onPlayerDied.Raise();
    }
}
```

* **反面教材（失敗例）**: 既存クラスに直接メソッドを生やすアプローチ（`GameManager.instance.UpdateUI()` など）は、機能追加のたびにAIが GameManager.cs 全体を書き直そうとし、ドミノ倒し式にコンパイルエラーを連鎖させる。SOアーキテクチャなら「ファイルが分離」されているため、AIは新しいコマンドの処理だけを独立したクラスとして実装でき、既存コードが壊れない。

---

## Q2：個人開発者とAIの役割分担とワークフロー

### 結論
**「人間がシーン配線と仕様決定」「AIが純粋なロジック関数とアセット生成」**という物理的な分離が必須。

### 成功事例：「35日・実質8人日でApp Store審査まで行った」ケース（2026年8月 Qiita）
「プログラムも絵も人は一行も作っていない」にもかかわらず、**「Unityのエディタ操作は人の手から切り離さない（＝AIに直接シーンをいじらせない）」**というルールを敷いた。人間は「良い要件仕様」を定義し、AIが生成したC#スクリプトやアセットをシーン上で組み立てる（接着剤の役割）に徹したことで破綻を防いだ。

### AIの分業と指示の粒度（2026年ベストプラクティス）
1. **設計・品質管理（Claude Opus等）**
   - 役割: 全体のアーキテクチャ設計、インターフェース定義、PRのコードレビュー。
   - 粒度: モジュール設計・指示書発行。
2. **実装（Gemini等）**
   - 役割: 定義されたインターフェースの中身（具象クラス）をひたすら書く。
   - 粒度: 「1つの機能（例：『休む』コマンドのステータス計算ロジック）を1つの純粋C#クラスで書け」という極小粒度。

* **反面教材（失敗例）**: AIに実行者と監督者の両方を任せ、実行者を無制限に走らせると、「早すぎる抽象化（無駄なインターフェースやマネージャーの量産）」を引き起こし、ゲームが一生遊べない状態に陥る。

---

## Q3：AIが書いたコードの品質を人間が疲弊せずに担保する方法

### 結論
**GitHub Actions (GameCI) による「PlayModeテストのCI自動実行」をゲートキーパーにする。**

2026年7月のZenn記事「AI駆動開発の1年 — 個人のアウトプットが9倍になるまでにやったこと」では、**「AIの検証を疑うゲートとレビューCIを整備した。ここを省くと、速くなった分だけ壊れたものが本流に入る」**と結論付けている。

### 現実的なCIパイプライン（個人開発向け）
EditModeテスト（純粋C#のテスト）はAIが無限にパスさせるが、シーンのバインド崩れは検知できない。必ず実シーンをロードする **PlayMode テスト**を導入する。

```csharp
[UnityTest]
public IEnumerator TurnProgression_Works_OnRealScene() {
    // 1. 本物のMainGameシーンをロード
    yield return SceneManager.LoadSceneAsync("MainGame");
    // 2. 本物のボタンを取得して押す（AIが参照を壊していればここで落ちる）
    var studyBtn = Object.FindFirstObjectByType<StudyCommandButton>();
    studyBtn.onClick.Invoke();
    yield return null;
    // 3. ターンが進んだか検証
    var flow = Object.FindFirstObjectByType<GameFlowController>();
    Assert.AreEqual(2, flow.CurrentTurn);
}
```

このテストが CI で Green にならない限り、AIのコミットを main ブランチにマージ（Push）させない物理的制約（Branch Protection Rule）を設ける。

---

## Q4：夜間・不在時に AI を自律で走らせる際の安全策

### 結論
完全な自律稼働（Auto Mode）は重大な破壊を招くため、**Git Worktree / 隔離ブランチによる「隔離」と、CIによる「朝のPRレポート」を組み合わせた非同期レビュー体制**が必須。

### 自律AIが引き起こす問題（2026年5月論文「What Breaks When LLMs Code?」）
自律AIエージェントの重大な失敗の **65%以上が「バグ修正」や「設定・構成（setup/config）」の作業中に発生**。AIはループが長くなるほどエラーを隠蔽・蓄積し（Safety Does Not Compose）、プロジェクトを破壊する。

### 安全なオーバーナイト（夜間）運用フロー
1. **Gitブランチの隔離**: 夜間にAIを走らせる際は、絶対に main ブランチを触らせず、専用の `nightly/` ブランチ上で動作させる。
2. **ループ回数の制限と停止条件**: 「最大試行回数」を設定し、それを超えたらエラーのまま終了（Safe-halt）する。
3. **PRでの朝のレポート**: AIの作業が終わったら自動でPRを作成させ、PlayMode テストとビルドを実行する。
4. **人間の朝の作業**: 翌朝、人間は「PlayModeテストが通っているか」と「WebGLビルドが遊べるか」だけを確認し、壊れていればPRを破棄（自動ロールバック）、成功していればマージする。
