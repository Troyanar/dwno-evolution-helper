using System.Collections.Generic;
using System.Globalization;

namespace DwnoEvoHelper;

/// UI texts in all game languages. English is the default; the user can pick a fixed language
/// or "auto" (follow the game language reported by the plugin).
/// Column order: de, en, fr, it, es, brp, jp, kr, tc.
public static class Loc
{
    static readonly string[] Codes = { "de", "en", "fr", "it", "es", "brp", "jp", "kr", "tc" };

    /// Selectable languages (code, native name). "auto" follows the game.
    public static readonly (string Code, string Name)[] Choices =
    {
        ("en", "English"), ("auto", "Auto"), ("de", "Deutsch"), ("fr", "Français"), ("it", "Italiano"),
        ("es", "Español"), ("brp", "Português (BR)"), ("jp", "日本語"), ("kr", "한국어"), ("tc", "繁體中文"),
    };

    /// User setting: a language code or "auto"
    public static string Choice { get; set; } = "en";

    public static string Lang { get; private set; } = "en";

    /// Applies the setting (and the game's language for "auto"). Returns true if the language changed.
    public static bool Set(string? gameCode)
    {
        var code = Choice == "auto" ? Normalize(gameCode) ?? "en" : Normalize(Choice) ?? "en";
        if (code == Lang) return false;
        Lang = code;
        return true;
    }

    /// The game's German table uses German colour suffixes; show the English names in the English UI.
    public static string DigimonName(string name, string? dataLang)
    {
        if (Lang != "en" || Normalize(dataLang) != "de") return name;
        return name.Replace("(Schwarz)", "(Black)").Replace("(Blau)", "(Blue)").Replace("(Silber)", "(Silver)")
                   .Replace("Phönixmon", "Phoenixmon").Replace("Toyagumon", "ToyAgumon");
    }

    static string? Normalize(string? c)
    {
        if (string.IsNullOrEmpty(c)) return null;
        c = c!.Trim().TrimStart('_').ToLowerInvariant(); // the game reports e.g. "_de"
        foreach (var code in Codes) if (c == code) return code;
        return c switch { "ja" => "jp", "ko" => "kr", "zh" or "zh-tw" or "sc" => "tc", "pt" or "pt-br" => "brp", _ => null };
    }

    public static string T(string key)
    {
        if (!Texts.TryGetValue(key, out var row)) return key;
        int i = System.Array.IndexOf(Codes, Lang);
        return i >= 0 && i < row.Length && !string.IsNullOrEmpty(row[i]) ? row[i] : row[1];
    }

    public static string F(string key, params object[] args) => string.Format(T(key), args);

    public static CultureInfo Culture => Lang switch
    {
        "de" => new CultureInfo("de-DE"), "fr" => new CultureInfo("fr-FR"), "it" => new CultureInfo("it-IT"),
        "es" => new CultureInfo("es-ES"), "brp" => new CultureInfo("pt-BR"), "jp" => new CultureInfo("ja-JP"),
        "kr" => new CultureInfo("ko-KR"), "tc" => new CultureInfo("zh-TW"), _ => new CultureInfo("en-US"),
    };

    static readonly Dictionary<string, string[]> Texts = new()
    {
        // de, en, fr, it, es, brp, jp, kr, tc
        ["title"] = new[] { "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper", "DWNO Evolution Helper" },
        ["st.live"] = new[] { "Live", "Live", "En direct", "Live", "En vivo", "Ao vivo", "ライブ", "실시간", "即時" },
        ["st.menu"] = new[] { "Im Menü", "In menu", "Au menu", "Nel menu", "En el menú", "No menu", "メニュー中", "메뉴", "選單中" },
        ["st.wait"] = new[] { "Warte auf Spiel", "Waiting for game", "En attente du jeu", "In attesa del gioco", "Esperando el juego", "Aguardando o jogo", "ゲーム待機中", "게임 대기 중", "等待遊戲" },
        ["st.off"] = new[] { "Spiel aus", "Game closed", "Jeu fermé", "Gioco chiuso", "Juego cerrado", "Jogo fechado", "ゲーム終了", "게임 종료", "遊戲已關閉" },
        ["st.paused"] = new[] { "Pausiert", "Paused", "En pause", "In pausa", "En pausa", "Pausado", "一時停止中", "일시 정지", "已暫停" },
        ["st.setup"] = new[] { "Einrichtung nötig", "Setup needed", "Installation requise", "Configurazione necessaria", "Configuración necesaria", "Configuração necessária", "セットアップが必要", "설정 필요", "需要設定" },
        ["partner"] = new[] { "Partner {0}", "Partner {0}", "Partenaire {0}", "Partner {0}", "Compañero {0}", "Parceiro {0}", "パートナー{0}", "파트너 {0}", "夥伴{0}" },
        ["finding.ready"] = new[] { "Erfüllt alles für {0}", "Meets everything for {0}", "Remplit tout pour {0}", "Soddisfa tutto per {0}", "Cumple todo para {0}", "Cumpre tudo para {0}", "{0}の条件をすべて達成", "{0} 조건 모두 충족", "已達成{0}的全部條件" },
        ["finding.closest"] = new[] { "Am nächsten: {0} ({1} von {2})", "Closest: {0} ({1} of {2})", "Le plus proche : {0} ({1} sur {2})", "Più vicino: {0} ({1} su {2})", "Más cerca: {0} ({1} de {2})", "Mais perto: {0} ({1} de {2})", "最も近い：{0}（{2}つ中{1}つ）", "가장 가까움: {0} ({2}개 중 {1}개)", "最接近：{0}（{2}項中{1}項）" },
        ["finding.missing"] = new[] { "fehlt: {0}", "missing: {0}", "manque : {0}", "manca: {0}", "falta: {0}", "falta: {0}", "不足：{0}", "부족: {0}", "還差：{0}" },
        ["timing.window"] = new[] { "Digitation ca. ab Alter {0} – noch {1} Tage", "Digivolves around age {0} – {1} days to go", "Digivolution vers l'âge {0} – encore {1} jours", "Digievoluzione verso l'età {0} – ancora {1} giorni", "Digievoluciona hacia la edad {0}: faltan {1} días", "Digievolui por volta da idade {0} – faltam {1} dias", "{0}歳ごろ進化 – あと{1}日", "{0}세쯤 진화 – {1}일 남음", "約{0}歲進化 – 還有{1}天" },
        ["timing.now"] = new[] { "Alter erreicht – digitiert beim Tageswechsel, sobald genug Bedingungen erfüllt sind", "Age reached – digivolves at the next day change once enough conditions are met", "Âge atteint – digivolue au changement de jour dès que les conditions sont remplies", "Età raggiunta – digievolve al cambio di giorno appena le condizioni sono soddisfatte", "Edad alcanzada: digievoluciona al cambiar el día cuando cumpla las condiciones", "Idade atingida – digievolui na virada do dia quando cumprir as condições", "年齢到達 – 条件を満たせば日付変更で進化", "나이 도달 – 조건을 채우면 날짜가 바뀔 때 진화", "已達年齡 – 滿足條件後於換日時進化" },
        ["timing.ready"] = new[] { "Bereit – digitiert beim nächsten Tageswechsel", "Ready – digivolves at the next day change", "Prêt – digivolue au prochain changement de jour", "Pronto – digievolve al prossimo cambio di giorno", "Listo: digievoluciona en el próximo cambio de día", "Pronto – digievolui na próxima virada do dia", "準備完了 – 次の日付変更で進化", "준비 완료 – 다음 날짜 변경 때 진화", "準備完成 – 下次換日時進化" },
        ["timing.soon"] = new[] { "Digitiert in wenigen Stunden", "Digivolves within a few hours", "Digivolue dans quelques heures", "Digievolve tra poche ore", "Digievoluciona en pocas horas", "Digievolui em poucas horas", "数時間以内に進化", "몇 시간 안에 진화", "幾小時內進化" },
        ["timing.final"] = new[] { "Endstufe – keine weitere normale Digitation", "Final stage – no further normal digivolution", "Stade final – plus de digivolution normale", "Stadio finale – nessun'altra digievoluzione normale", "Etapa final: no hay más digievoluciones normales", "Estágio final – sem outra digievolução normal", "最終段階 – 通常進化なし", "최종 단계 – 일반 진화 없음", "最終階段 – 沒有一般進化" },
        ["timing.nextday"] = new[] { "nächster Tag um {0} Spielzeit", "next day at {0} game time", "jour suivant à {0} (heure du jeu)", "giorno successivo alle {0} (ora di gioco)", "día siguiente a las {0} (hora del juego)", "próximo dia às {0} (hora do jogo)", "次の日はゲーム内{0}", "다음 날은 게임 시간 {0}", "下一天於遊戲時間{0}" },
        ["timing.fallback"] = new[] { "bis Alter {0} sonst {1}", "otherwise {1} by age {0}", "sinon {1} à l'âge {0}", "altrimenti {1} entro l'età {0}", "si no, {1} a la edad {0}", "senão {1} até a idade {0}", "{0}歳までに満たさないと{1}", "{0}세까지 못 채우면 {1}", "否則{0}歲時變成{1}" },
        ["timing.min"] = new[] { "{0} min", "{0} min", "{0} min", "{0} min", "{0} min", "{0} min", "{0}分", "{0}분", "{0}分鐘" },
        ["timing.hmin"] = new[] { "{0} h {1} min", "{0} h {1} min", "{0} h {1} min", "{0} h {1} min", "{0} h {1} min", "{0} h {1} min", "{0}時間{1}分", "{0}시간 {1}분", "{0}小時{1}分" },
        ["timing.lessmin"] = new[] { "unter 1 min", "under 1 min", "moins d'1 min", "meno di 1 min", "menos de 1 min", "menos de 1 min", "1分未満", "1분 미만", "不到1分鐘" },
        ["life"] = new[] { "Leben ~{0} Tage", "Life ~{0} days", "Vie ~{0} jours", "Vita ~{0} giorni", "Vida ~{0} días", "Vida ~{0} dias", "寿命 約{0}日", "수명 약 {0}일", "壽命約{0}天" },
        ["timing.nextday.real"] = new[] { "nächster Tag um {0} Spielzeit (ca. {1})", "next day at {0} game time (about {1})", "jour suivant à {0} (heure du jeu, env. {1})", "giorno successivo alle {0} (ora di gioco, circa {1})", "día siguiente a las {0} (hora del juego, aprox. {1})", "próximo dia às {0} (hora do jogo, cerca de {1})", "次の日はゲーム内{0}（約{1}）", "다음 날은 게임 시간 {0} (약 {1})", "下一天於遊戲時間{0}（約{1}）" },
        ["finding.multi"] = new[] { "Erfüllt: {0} – welches das Spiel wählt, ist nicht sicher", "Met: {0} – which one the game picks is uncertain", "Remplis : {0} – le choix du jeu est incertain", "Soddisfatti: {0} – la scelta del gioco è incerta", "Cumplidos: {0}: no es seguro cuál elige el juego", "Cumpridos: {0} – não é certo qual o jogo escolhe", "達成：{0} – どれになるかは不確定", "충족: {0} – 게임이 무엇을 고를지는 불확실", "已達成：{0} – 遊戲會選哪個不確定" },
        ["finding.fragile"] = new[] { "knapp: {0}", "close to the limit: {0}", "juste : {0}", "al limite: {0}", "justo: {0}", "no limite: {0}", "ギリギリ：{0}", "아슬아슬: {0}", "接近上限：{0}" },
        ["priority"] = new[] { "Priorität laut Spiel: {0}", "Game priority: {0}", "Priorité du jeu : {0}", "Priorità del gioco: {0}", "Prioridad del juego: {0}", "Prioridade do jogo: {0}", "ゲーム内優先度：{0}", "게임 우선순위: {0}", "遊戲優先度：{0}" },
        ["badge.choice"] = new[] { "Wahl des Spiels", "Game's choice", "Choix du jeu", "Scelta del gioco", "Elección del juego", "Escolha do jogo", "ゲームの選択", "게임의 선택", "遊戲的選擇" },
        ["finding.choice"] = new[] { "Laut Spiel: digitiert zu {0}", "According to the game: digivolves into {0}", "Selon le jeu : digivolue en {0}", "Secondo il gioco: digievolve in {0}", "Según el juego: digievoluciona a {0}", "Segundo o jogo: digievolui para {0}", "ゲーム判定：{0}に進化", "게임 판정: {0}(으)로 진화", "遊戲判定：進化成{0}" },
        ["finding.nochoice"] = new[] { "Laut Spiel: zurzeit keine Digitation", "According to the game: no digivolution right now", "Selon le jeu : aucune digivolution pour l'instant", "Secondo il gioco: nessuna digievoluzione ora", "Según el juego: ahora no hay digievolución", "Segundo o jogo: nenhuma digievolução agora", "ゲーム判定：現在は進化なし", "게임 판정: 현재 진화 없음", "遊戲判定：目前不會進化" },
        ["prio.short"] = new[] { "Prio {0}", "Prio {0}", "Prio {0}", "Prio {0}", "Prio {0}", "Prio {0}", "優先 {0}", "우선 {0}", "優先 {0}" },
        ["mode.log"] = new[] { "Protokoll", "Log", "Journal", "Registro", "Registro", "Registro", "記録", "기록", "紀錄" },
        ["log.title"] = new[] { "Digitations-Protokoll", "Digivolution log", "Journal des digivolutions", "Registro digievoluzioni", "Registro de digievoluciones", "Registro de digievoluções", "進化の記録", "진화 기록", "進化紀錄" },
        ["log.empty"] = new[] { "Noch keine Digitation aufgezeichnet. Das Plugin schreibt jede Digitation automatisch mit – auch wenn die App geschlossen ist.", "No digivolution recorded yet. The plugin records every digivolution automatically – even with the app closed.", "Aucune digivolution enregistrée. Le plugin les enregistre automatiquement, même app fermée.", "Nessuna digievoluzione registrata. Il plugin le registra automaticamente, anche ad app chiusa.", "Aún no hay digievoluciones. El plugin las registra automáticamente, incluso con la app cerrada.", "Nenhuma digievolução registrada. O plugin registra automaticamente, mesmo com o app fechado.", "まだ記録はありません。プラグインがアプリを閉じていても自動で記録します。", "아직 기록이 없습니다. 플러그인이 앱이 꺼져 있어도 자동으로 기록합니다.", "尚無紀錄。外掛會自動記錄每次進化，即使程式已關閉。" },
        ["log.count"] = new[] { "{0} Digitationen aufgezeichnet, davon {1} mit mehreren erfüllten Zielen (diese entscheiden, welche Regel stimmt).", "{0} digivolutions recorded, {1} with several targets met (these decide which rule is right).", "{0} digivolutions enregistrées, dont {1} avec plusieurs cibles remplies (elles tranchent).", "{0} digievoluzioni registrate, {1} con più obiettivi soddisfatti (decidono la regola).", "{0} digievoluciones registradas, {1} con varios objetivos cumplidos (deciden la regla).", "{0} digievoluções registradas, {1} com vários alvos cumpridos (decidem a regra).", "{0}件記録、うち複数の条件達成は{1}件（ルール判定に有効）。", "{0}건 기록, 그중 여러 목표 충족 {1}건 (규칙 판정에 결정적).", "已記錄{0}次，其中{1}次同時滿足多個目標（可判定規則）。" },
        ["log.score"] = new[] { "{0}: {1} von {2} richtig · bei mehreren erfüllten: {3} von {4}", "{0}: {1} of {2} correct · with several met: {3} of {4}", "{0} : {1} sur {2} · plusieurs remplies : {3} sur {4}", "{0}: {1} su {2} · con più soddisfatti: {3} su {4}", "{0}: {1} de {2} · con varios cumplidos: {3} de {4}", "{0}: {1} de {2} · com vários cumpridos: {3} de {4}", "{0}：{2}件中{1}件正解 · 複数達成時：{4}件中{3}件", "{0}: {2}건 중 {1}건 정답 · 여러 충족: {4}건 중 {3}건", "{0}：{2}次中{1}次正確 · 多個滿足時：{4}次中{3}次" },
        ["log.none"] = new[] { "Kein Ziel war voll erfüllt – Fall für die Ersatz-Digitation", "No target was fully met – fallback digivolution", "Aucune cible remplie – digivolution de repli", "Nessun obiettivo soddisfatto – digievoluzione di ripiego", "Ningún objetivo cumplido: digievolución de reserva", "Nenhum alvo cumprido – digievolução substituta", "条件達成なし – 代替進化", "충족된 목표 없음 – 대체 진화", "沒有滿足的目標 – 替代進化" },
        ["log.keycurrent"] = new[] { "Schlüssel = aktuelle Form", "key = current form", "clé = forme actuelle", "chiave = forma attuale", "clave = forma actual", "chave = forma atual", "キー＝現在の姿", "키 = 현재 형태", "關鍵＝目前形態" },
        ["log.report"] = new[] { "Bericht als Text öffnen", "Open report as text", "Ouvrir le rapport texte", "Apri il resoconto", "Abrir informe de texto", "Abrir relatório", "テキストで開く", "텍스트 보고서 열기", "開啟文字報告" },
        ["rule.surplus"] = new[] { "Überschuss-Regel", "Surplus rule", "Règle du surplus", "Regola del surplus", "Regla del excedente", "Regra do excedente", "超過数ルール", "초과 규칙", "超額規則" },
        ["rule.prio"] = new[] { "Höchste Prio", "Highest priority", "Priorité max.", "Priorità più alta", "Prioridad más alta", "Maior prioridade", "最高優先度", "최고 우선순위", "最高優先度" },
        ["rule.list"] = new[] { "Listenreihenfolge", "List order", "Ordre de la liste", "Ordine della lista", "Orden de la lista", "Ordem da lista", "リスト順", "목록 순서", "列表順序" },
        ["rule.choice"] = new[] { "Spiel-Wahl kurz davor", "Game's pick just before", "Choix du jeu juste avant", "Scelta del gioco poco prima", "Elección del juego justo antes", "Escolha do jogo logo antes", "直前のゲーム判定", "직전 게임 판정", "前一刻的遊戲判定" },
        ["badge.predicted"] = new[] { "Vorhersage", "Prediction", "Prédiction", "Previsione", "Predicción", "Previsão", "予測", "예측", "預測" },
        ["finding.predicted"] = new[] { "Voraussichtlich: {0} ({1} über Soll)", "Expected: {0} ({1} over the quota)", "Prévu : {0} ({1} au-delà)", "Previsto: {0} ({1} oltre la soglia)", "Previsto: {0} ({1} sobre lo necesario)", "Previsto: {0} ({1} acima do necessário)", "予測：{0}（必要数より{1}）", "예상: {0} (필요보다 {1})", "預計：{0}（超出{1}）" },
        ["finding.agree"] = new[] { "Vorhersage stimmt überein", "prediction agrees", "la prédiction concorde", "la previsione coincide", "la predicción coincide", "a previsão confere", "予測と一致", "예측과 일치", "與預測一致" },
        ["rule.fallback"] = new[] { "Ersatz: am nächsten dran", "Fallback: closest", "Repli : le plus proche", "Ripiego: il più vicino", "Reserva: el más cercano", "Substituto: o mais próximo", "代替：最も近い", "대체: 가장 가까운", "替代：最接近" },
        ["log.fallbackscore"] = new[] { "{0}: {1} von {2} richtig (Fälle ohne erfülltes Ziel)", "{0}: {1} of {2} correct (cases with no target met)", "{0} : {1} sur {2} (aucune cible remplie)", "{0}: {1} su {2} (nessun obiettivo soddisfatto)", "{0}: {1} de {2} (sin objetivo cumplido)", "{0}: {1} de {2} (nenhum alvo cumprido)", "{0}：{2}件中{1}件（達成なしの場合）", "{0}: {2}건 중 {1}건 (충족 없음)", "{0}：{2}次中{1}次（無滿足目標時）" },
        ["train.plan"] = new[] { "Training: {0}", "Training: {0}", "Entraînement : {0}", "Allenamento: {0}", "Entrenamiento: {0}", "Treino: {0}", "トレーニング：{0}", "훈련: {0}", "訓練：{0}" },
        ["train.for"] = new[] { "Trainingsplan für {0}: {1}", "Training plan for {0}: {1}", "Plan d'entraînement pour {0} : {1}", "Piano di allenamento per {0}: {1}", "Plan de entrenamiento para {0}: {1}", "Plano de treino para {0}: {1}", "{0}へのトレーニング計画：{1}", "{0} 훈련 계획: {1}", "{0}的訓練計畫：{1}" },
        ["train.measured"] = new[] { "(aus {0} gemessenen Einheiten)", "(from {0} measured sessions)", "(d'après {0} séances mesurées)", "(da {0} sessioni misurate)", "(según {0} sesiones medidas)", "(de {0} sessões medidas)", "（計測{0}回分）", "({0}회 측정 기준)", "（依{0}次實測）" },
        ["train.unmeasured"] = new[] { "noch nicht gemessen: {0}-Training", "not measured yet: {0} training", "pas encore mesuré : entraînement {0}", "non ancora misurato: allenamento {0}", "aún sin medir: entrenamiento {0}", "ainda não medido: treino {0}", "未計測：{0}トレーニング", "미측정: {0} 훈련", "尚未實測：{0}訓練" },
        ["train.first"] = new[] { "Trainingsplan: Trainiere einmal, dann rechnet die App mit deinen echten Zuwächsen.", "Training plan: train once and the app calculates with your real gains.", "Plan d'entraînement : entraîne-toi une fois, l'app calculera avec tes vrais gains.", "Piano di allenamento: allenati una volta e l'app userà i tuoi guadagni reali.", "Plan de entrenamiento: entrena una vez y la app usará tus ganancias reales.", "Plano de treino: treine uma vez e o app calcula com seus ganhos reais.", "トレーニング計画：一度トレーニングすると実際の上昇値で計算します。", "훈련 계획: 한 번 훈련하면 실제 상승치로 계산합니다.", "訓練計畫：先訓練一次，程式會以實際成長計算。" },
        ["warn.dying"] = new[] { "Lebenszeit fast vorbei (unter 1 Tag)", "Lifetime almost over (under 1 day)", "Fin de vie proche (moins d'1 jour)", "Vita quasi finita (meno di 1 giorno)", "La vida casi termina (menos de 1 día)", "Vida quase no fim (menos de 1 dia)", "寿命残りわずか（1日未満）", "수명이 거의 끝남 (1일 미만)", "壽命即將結束（不到1天）" },
        ["warn.life"] = new[] { "Leben ~{0} Tage – reicht evtl. nicht bis zur nächsten Digitation in ~{1} Tagen", "Life ~{0} days – may not last until the next digivolution in ~{1} days", "Vie ~{0} jours – peut-être insuffisante avant la prochaine digivolution (~{1} jours)", "Vita ~{0} giorni – forse non basta fino alla prossima digievoluzione (~{1} giorni)", "Vida ~{0} días: quizá no llegue a la próxima digievolución (~{1} días)", "Vida ~{0} dias – talvez não chegue à próxima digievolução (~{1} dias)", "寿命約{0}日 – 次の進化（約{1}日後）まで持たない可能性", "수명 약 {0}일 – 다음 진화(약 {1}일 후)까지 못 버틸 수 있음", "壽命約{0}天 – 可能撐不到下次進化（約{1}天後）" },
        ["warn.agegap"] = new[] { "Alter weicht um {0} Tage von {1} ab – Digitationen fallen auseinander", "Age differs from {1} by {0} days – digivolutions drift apart", "Âge différent de {1} de {0} jours – les digivolutions se décalent", "Età diversa da {1} di {0} giorni – le digievoluzioni si sfasano", "La edad difiere de {1} en {0} días: las digievoluciones se separan", "Idade difere de {1} em {0} dias – as digievoluções se separam", "{1}と年齢が{0}日ずれています – 進化時期がずれます", "{1}와 나이 차이 {0}일 – 진화 시기가 어긋남", "與{1}年齡差{0}天 – 進化時間會錯開" },
        ["hk.clickthrough"] = new[] { "Durchklicken an · Strg+Umschalt+D", "Click-through on · Ctrl+Shift+D", "Clics traversants · Ctrl+Maj+D", "Clic passante · Ctrl+Maiusc+D", "Clic a través · Ctrl+Mayús+D", "Clique atravessa · Ctrl+Shift+D", "クリック透過中 · Ctrl+Shift+D", "클릭 통과 · Ctrl+Shift+D", "點擊穿透 · Ctrl+Shift+D" },
        ["hk.info"] = new[] { "Tastenkürzel (auch im Spiel): Strg+Umschalt+E blendet das Fenster ein/aus · Strg+Umschalt+D schaltet Durchklicken an/aus (Mausklicks gehen ins Spiel)", "Hotkeys (also in game): Ctrl+Shift+E shows/hides the window · Ctrl+Shift+D toggles click-through (clicks go to the game)", "Raccourcis (aussi en jeu) : Ctrl+Maj+E affiche/masque · Ctrl+Maj+D active les clics traversants", "Scorciatoie (anche in gioco): Ctrl+Maiusc+E mostra/nasconde · Ctrl+Maiusc+D clic passante", "Atajos (también en el juego): Ctrl+Mayús+E muestra/oculta · Ctrl+Mayús+D clic a través", "Atalhos (também no jogo): Ctrl+Shift+E mostra/oculta · Ctrl+Shift+D clique atravessa", "ショートカット（ゲーム中も可）：Ctrl+Shift+E 表示/非表示 · Ctrl+Shift+D クリック透過", "단축키(게임 중에도): Ctrl+Shift+E 표시/숨김 · Ctrl+Shift+D 클릭 통과", "快捷鍵（遊戲中亦可）：Ctrl+Shift+E 顯示/隱藏 · Ctrl+Shift+D 點擊穿透" },
        ["update.available"] = new[] { "Update {0} verfügbar", "Update {0} available", "Mise à jour {0} disponible", "Aggiornamento {0} disponibile", "Actualización {0} disponible", "Atualização {0} disponível", "更新 {0} あり", "업데이트 {0} 있음", "有新版本 {0}" },
        ["lang.title"] = new[] { "Sprache", "Language", "Langue", "Lingua", "Idioma", "Idioma", "言語", "언어", "語言" },
        ["lang.auto"] = new[] { "Auto = wie das Spiel", "Auto = same as the game", "Auto = comme le jeu", "Auto = come il gioco", "Auto = como el juego", "Auto = como o jogo", "自動＝ゲームと同じ", "자동 = 게임과 동일", "自動＝與遊戲相同" },
        ["timing.tooltip"] = new[] { "Richtwerte aus der Community – das Spiel prüft die Digitation beim Tageswechsel.", "Community estimates – the game checks digivolution at the day change.", "Estimations de la communauté – le jeu vérifie au changement de jour.", "Stime della community – il gioco controlla al cambio di giorno.", "Estimaciones de la comunidad: el juego comprueba al cambiar el día.", "Estimativas da comunidade – o jogo verifica na virada do dia.", "コミュニティの目安 – 判定は日付変更時。", "커뮤니티 추정치 – 날짜가 바뀔 때 판정합니다.", "社群估計值 – 遊戲於換日時判定。" },
        ["both"] = new[] { "Beide", "Both", "Les deux", "Entrambi", "Ambos", "Ambos", "両方", "둘 다", "兩者" },
        ["age"] = new[] { "Alter {0}", "Age {0}", "Âge {0}", "Età {0}", "Edad {0}", "Idade {0}", "年齢 {0}", "나이 {0}", "年齡 {0}" },
        ["live"] = new[] { "live", "live", "en direct", "live", "en vivo", "ao vivo", "ライブ", "실시간", "即時" },
        ["asof"] = new[] { "Stand {0}", "As of {0}", "État à {0}", "Stato delle {0}", "Estado a las {0}", "Estado às {0}", "{0} 時点", "{0} 기준", "{0} 時的狀態" },
        ["weight"] = new[] { "Gewicht", "Weight", "Poids", "Peso", "Peso", "Peso", "体重", "체중", "體重" },
        ["mistakes"] = new[] { "Fehler", "Mistakes", "Erreurs", "Errori", "Errores", "Erros", "ミス", "실수", "失誤" },
        ["bond"] = new[] { "Bindung", "Bond", "Lien", "Legame", "Vínculo", "Vínculo", "絆", "유대", "羈絆" },
        ["discipline"] = new[] { "Disziplin", "Discipline", "Discipline", "Disciplina", "Disciplina", "Disciplina", "しつけ", "훈육", "管教" },
        ["wins"] = new[] { "Siege", "Wins", "Victoires", "Vittorie", "Victorias", "Vitórias", "勝利数", "승리", "勝場" },
        ["key"] = new[] { "Schlüssel", "Key", "Clé", "Chiave", "Clave", "Chave", "キー", "키", "關鍵" },
        ["sec.next"] = new[] { "Nächste Digitationen", "Next digivolutions", "Prochaines digivolutions", "Prossime digievoluzioni", "Próximas digievoluciones", "Próximas digievoluções", "次の進化", "다음 진화", "下一個進化" },
        ["sec.plan"] = new[] { "Ziel-Digimon planen", "Plan a target Digimon", "Planifier un Digimon cible", "Pianifica un Digimon", "Planear Digimon objetivo", "Planejar Digimon alvo", "目標デジモンを計画", "목표 디지몬 계획", "規劃目標數碼寶貝" },
        ["evo.nodata"] = new[] { "Keine Daten – starte das Spiel und lade einen Spielstand.", "No data – start the game and load a save.", "Aucune donnée – lance le jeu et charge une sauvegarde.", "Nessun dato – avvia il gioco e carica un salvataggio.", "Sin datos: inicia el juego y carga una partida.", "Sem dados – inicie o jogo e carregue um save.", "データなし – ゲームを起動してセーブデータをロードしてください。", "데이터 없음 – 게임을 실행하고 세이브를 불러오세요.", "沒有資料 – 請啟動遊戲並讀取存檔。" },
        ["evo.final"] = new[] { "Keine weitere normale Digitation (Endstufe oder nur Jogress/ExE).", "No further normal digivolution (final stage or Jogress/ExE only).", "Plus de digivolution normale (stade final ou Jogress/ExE uniquement).", "Nessun'altra digievoluzione normale (stadio finale o solo Jogress/ExE).", "No hay más digievoluciones normales (etapa final o solo Jogress/ExE).", "Nenhuma digievolução normal (estágio final ou só Jogress/ExE).", "通常進化なし（最終段階、またはジョグレス/ExEのみ）。", "일반 진화 없음 (최종 단계 또는 조그레스/ExE 전용).", "沒有一般進化（最終階段或僅限合體/ExE）。" },
        ["search"] = new[] { "Digimon suchen, z. B. WarGreymon …", "Search Digimon, e.g. WarGreymon …", "Chercher un Digimon, ex. WarGreymon …", "Cerca Digimon, es. WarGreymon …", "Buscar Digimon, p. ej. WarGreymon …", "Buscar Digimon, ex. WarGreymon …", "デジモンを検索（例：ウォーグレイモン）", "디지몬 검색 (예: 워그레이몬)", "搜尋數碼寶貝（例：戰鬥暴龍獸）" },
        ["plan.already"] = new[] { "Dein Partner ist bereits {0}.", "Your partner already is {0}.", "Ton partenaire est déjà {0}.", "Il tuo partner è già {0}.", "Tu compañero ya es {0}.", "Seu parceiro já é {0}.", "パートナーはすでに{0}です。", "파트너는 이미 {0}입니다.", "你的夥伴已經是{0}。" },
        ["plan.unreach"] = new[] { "Von {0} aus nicht über normale Digitation erreichbar.", "Not reachable from {0} via normal digivolution.", "Inaccessible depuis {0} par digivolution normale.", "Non raggiungibile da {0} con digievoluzione normale.", "No se puede alcanzar desde {0} con digievolución normal.", "Inalcançável a partir de {0} por digievolução normal.", "{0}から通常進化では到達できません。", "{0}에서 일반 진화로는 도달할 수 없습니다.", "無法從{0}透過一般進化到達。" },
        ["plan.from"] = new[] { "Entsteht aus: {0}", "Comes from: {0}", "Vient de : {0}", "Deriva da: {0}", "Proviene de: {0}", "Vem de: {0}", "進化元：{0}", "진화 전: {0}", "進化來源：{0}" },
        ["plan.special"] = new[] { "Nur über Jogress/Spezialereignisse.", "Only via Jogress/special events.", "Uniquement par Jogress/événements spéciaux.", "Solo tramite Jogress/eventi speciali.", "Solo mediante Jogress/eventos especiales.", "Apenas via Jogress/eventos especiais.", "ジョグレス/特殊イベントのみ。", "조그레스/특수 이벤트로만 가능.", "僅能透過合體/特殊事件。" },
        ["your.partner"] = new[] { "deinem Partner", "your partner", "ton partenaire", "il tuo partner", "tu compañero", "seu parceiro", "パートナー", "파트너", "你的夥伴" },
        ["footer"] = new[] { "Bedingungen direkt aus den Spieldaten · Sind genug erfüllt, digitiert dein Partner beim Tageswechsel. Sind mehrere Ziele erfüllt, entscheidet das Spiel selbst.", "Conditions read from the game data · With enough met, your partner digivolves at the day change. If several targets are met, the game decides.", "Conditions lues dans les données du jeu · Assez de conditions remplies : digivolution au changement de jour. Si plusieurs cibles sont remplies, le jeu décide.", "Condizioni lette dai dati di gioco · Con abbastanza condizioni il partner digievolve al cambio di giorno. Se più obiettivi sono soddisfatti, decide il gioco.", "Condiciones leídas de los datos del juego · Con suficientes cumplidas, digievoluciona al cambiar el día. Si se cumplen varios objetivos, decide el juego.", "Condições lidas dos dados do jogo · Com condições suficientes, digievolui na virada do dia. Se vários alvos forem cumpridos, o jogo decide.", "条件はゲームデータから取得 · 条件を満たすと日付変更時に進化。複数満たした場合はゲームが決定。", "조건은 게임 데이터에서 읽음 · 충분히 충족하면 날짜가 바뀔 때 진화. 여러 개면 게임이 결정합니다.", "條件讀取自遊戲資料 · 滿足足夠條件即於換日時進化。若滿足多個目標，由遊戲決定。" },
        ["badge.met"] = new[] { "Erfüllt", "Met", "Rempli", "Soddisfatto", "Cumplido", "Cumprido", "達成", "충족", "已達成" },
        ["hint"] = new[] { "Noch {0} fehlend – am nächsten dran: {1}", "{0} still missing – closest: {1}", "Encore {0} manquante(s) – les plus proches : {1}", "Ne mancano {0} – più vicine: {1}", "Faltan {0} – más cercanas: {1}", "Faltam {0} – mais próximas: {1}", "あと{0}つ – 近いもの：{1}", "{0}개 부족 – 가장 가까운 것: {1}", "還差{0}項 – 最接近：{1}" },
        ["min"] = new[] { "mindestens {0}", "at least {0}", "au moins {0}", "almeno {0}", "al menos {0}", "pelo menos {0}", "{0}以上", "{0} 이상", "至少{0}" },
        ["max"] = new[] { "höchstens {0}", "at most {0}", "au plus {0}", "al massimo {0}", "como máximo {0}", "no máximo {0}", "{0}以下", "{0} 이하", "最多{0}" },
        ["maxShort"] = new[] { "max", "max", "max", "max", "máx", "máx", "最大", "최대", "最多" },
        ["met"] = new[] { "erfüllt", "met", "rempli", "soddisfatto", "cumplido", "cumprido", "達成", "충족", "已達成" },
        ["missing"] = new[] { "+{0} nötig", "+{0} needed", "+{0} requis", "+{0} necessari", "faltan +{0}", "faltam +{0}", "あと{0}", "{0} 더 필요", "還需{0}" },
        ["toomuch"] = new[] { "{0} zu viel", "{0} too many", "{0} de trop", "{0} di troppo", "{0} de más", "{0} a mais", "{0}多い", "{0} 초과", "超過{0}" },
        ["key.met"] = new[] { "Schlüssel-Digimon im Werdegang – erfüllt", "Key Digimon in your partner's history – met", "Digimon clé dans l'historique – rempli", "Digimon chiave nella storia – soddisfatto", "Digimon clave en el historial – cumplido", "Digimon chave no histórico – cumprido", "キーデジモンは経歴にあり – 達成", "키 디지몬이 이력에 있음 – 충족", "關鍵數碼寶貝在經歷中 – 已達成" },
        ["key.need"] = new[] { "Dein Partner muss vorher {0} gewesen sein", "Your partner must have been {0} before", "Ton partenaire doit avoir été {0} avant", "Il partner deve essere stato {0} prima", "Tu compañero debe haber sido {0} antes", "Seu parceiro precisa ter sido {0} antes", "以前に{0}だった必要があります", "이전에 {0}였어야 합니다", "夥伴之前必須是{0}" },
        ["key.short"] = new[] { "vorher {0}", "was {0}", "avoir été {0}", "essere stato {0}", "haber sido {0}", "ter sido {0}", "{0}経由", "{0} 경유", "經過{0}" },
        ["noPartner"] = new[] { "Lade einen Spielstand, dann erscheinen hier deine Partner.", "Load a save and your partners will show up here.", "Charge une sauvegarde pour voir tes partenaires ici.", "Carica un salvataggio per vedere qui i tuoi partner.", "Carga una partida y aquí aparecerán tus compañeros.", "Carregue um save e seus parceiros aparecerão aqui.", "セーブデータをロードするとパートナーが表示されます。", "세이브를 불러오면 파트너가 여기에 표시됩니다.", "讀取存檔後，夥伴會顯示在這裡。" },
        ["noData"] = new[] { "Starte das Spiel einmal mit installiertem Plugin.", "Start the game once with the plugin installed.", "Lance le jeu une fois avec le plugin installé.", "Avvia il gioco una volta con il plugin installato.", "Inicia el juego una vez con el plugin instalado.", "Inicie o jogo uma vez com o plugin instalado.", "プラグインを導入した状態でゲームを一度起動してください。", "플러그인을 설치한 상태로 게임을 한 번 실행하세요.", "請在安裝外掛後啟動遊戲一次。" },
        ["icons.wait"] = new[] { "Bilder werden aus dem Spiel geladen …", "Loading pictures from the game …", "Chargement des images du jeu …", "Caricamento immagini dal gioco …", "Cargando imágenes del juego …", "Carregando imagens do jogo …", "ゲームから画像を読み込み中…", "게임에서 이미지를 불러오는 중…", "正在從遊戲載入圖片…" },
        ["tip.pin"] = new[] { "Immer im Vordergrund", "Always on top", "Toujours au premier plan", "Sempre in primo piano", "Siempre visible", "Sempre no topo", "常に手前に表示", "항상 위", "永遠在最上層" },
        ["tip.opacity"] = new[] { "Deckkraft", "Opacity", "Opacité", "Opacità", "Opacidad", "Opacidade", "不透明度", "불투명도", "不透明度" },
        ["tip.settings"] = new[] { "Einrichtung", "Setup", "Installation", "Configurazione", "Configuración", "Configuração", "セットアップ", "설정", "設定" },
        ["tip.min"] = new[] { "Minimieren", "Minimize", "Réduire", "Riduci", "Minimizar", "Minimizar", "最小化", "최소화", "最小化" },
        ["tip.close"] = new[] { "Schließen", "Close", "Fermer", "Chiudi", "Cerrar", "Fechar", "閉じる", "닫기", "關閉" },

        // Digidex / planning
        ["mode.partner"] = new[] { "Partner", "Partners", "Partenaires", "Partner", "Compañeros", "Parceiros", "パートナー", "파트너", "夥伴" },
        ["mode.dex"] = new[] { "Digidex", "Digidex", "Digidex", "Digidex", "Digidex", "Digidex", "デジデックス", "디지덱스", "數碼圖鑑" },
        ["dex.search"] = new[] { "Im Digidex suchen …", "Search the Digidex …", "Chercher dans le Digidex …", "Cerca nel Digidex …", "Buscar en el Digidex …", "Buscar no Digidex …", "デジデックスを検索…", "디지덱스 검색…", "搜尋圖鑑…" },
        ["dex.all"] = new[] { "Alle", "All", "Tous", "Tutti", "Todos", "Todos", "すべて", "전체", "全部" },
        ["dex.count"] = new[] { "{0} Digimon", "{0} Digimon", "{0} Digimon", "{0} Digimon", "{0} Digimon", "{0} Digimon", "{0}体", "{0}마리", "{0} 隻" },
        ["dex.from"] = new[] { "Entsteht aus", "Comes from", "Vient de", "Deriva da", "Proviene de", "Vem de", "進化元", "진화 전", "進化來源" },
        ["dex.to"] = new[] { "Digitiert zu", "Digivolves into", "Digivolue en", "Digievolve in", "Digievoluciona a", "Digievolui para", "進化先", "진화 후", "可進化為" },
        ["dex.req"] = new[] { "Bedingungen für {0}", "Requirements for {0}", "Conditions pour {0}", "Condizioni per {0}", "Condiciones para {0}", "Condições para {0}", "{0}への進化条件", "{0} 진화 조건", "進化成{0}的條件" },
        ["dex.path"] = new[] { "Weg von {0}", "Path from {0}", "Chemin depuis {0}", "Percorso da {0}", "Camino desde {0}", "Caminho a partir de {0}", "{0}からのルート", "{0}에서의 경로", "從{0}出發的路線" },
        ["dex.needed"] = new[] { "{0} von {1} nötig", "{0} of {1} needed", "{0} sur {1} requises", "{0} su {1} necessarie", "{0} de {1} necesarias", "{0} de {1} necessárias", "{1}つ中{0}つ必要", "{1}개 중 {0}개 필요", "需{1}項中的{0}項" },
        ["dex.noreq"] = new[] { "Keine Bedingungen – entsteht aus dem Digi-Ei bzw. automatisch.", "No requirements – hatches from an egg or evolves automatically.", "Aucune condition – éclot d'un œuf ou évolue automatiquement.", "Nessuna condizione – nasce da un uovo o evolve automaticamente.", "Sin condiciones: nace de un huevo o evoluciona automáticamente.", "Sem condições – nasce de um ovo ou evolui automaticamente.", "条件なし – タマゴから誕生、または自動で進化。", "조건 없음 – 알에서 태어나거나 자동으로 진화.", "無條件 – 由蛋孵化或自動進化。" },
        ["dex.noreq.short"] = new[] { "keine Bedingungen", "no requirements", "aucune condition", "nessuna condizione", "sin condiciones", "sem condições", "条件なし", "조건 없음", "無條件" },
        ["dex.special.short"] = new[] { "nur über Jogress/Spezialereignisse", "Jogress/special events only", "Jogress/événements spéciaux uniquement", "solo Jogress/eventi speciali", "solo Jogress/eventos especiales", "apenas Jogress/eventos especiais", "ジョグレス/特殊イベントのみ", "조그레스/특수 이벤트 전용", "僅限合體/特殊事件" },
        ["dex.compare"] = new[] { "Mit {0} vergleichen", "Compare with {0}", "Comparer avec {0}", "Confronta con {0}", "Comparar con {0}", "Comparar com {0}", "{0}と比較", "{0}와 비교", "與{0}比較" },
        ["dex.final"] = new[] { "Keine weitere normale Digitation.", "No further normal digivolution.", "Aucune autre digivolution normale.", "Nessun'altra digievoluzione normale.", "No hay más digievoluciones normales.", "Nenhuma outra digievolução normal.", "通常進化なし。", "일반 진화 없음.", "沒有一般進化。" },
        ["dex.start"] = new[] { "Startform – entsteht aus dem Digi-Ei.", "Starting form – hatches from a Digi-Egg.", "Forme de départ – éclot d'un Digi-Œuf.", "Forma iniziale – nasce da un Digi-Uovo.", "Forma inicial: nace de un Digi-Huevo.", "Forma inicial – nasce de um Digi-Ovo.", "初期形態 – デジタマから誕生。", "시작 형태 – 디지알에서 태어남.", "初始形態 – 由數碼蛋孵化。" },
        ["dex.back"] = new[] { "Zurück", "Back", "Retour", "Indietro", "Atrás", "Voltar", "戻る", "뒤로", "返回" },

        // Stages
        ["stage0"] = new[] { "Baby I", "Baby I", "Bébé I", "Primo stadio", "Bebé I", "Bebê I", "幼年期Ⅰ", "유년기 I", "幼年期Ⅰ" },
        ["stage1"] = new[] { "Baby II", "Baby II", "Bébé II", "Secondo stadio", "Bebé II", "Bebê II", "幼年期Ⅱ", "유년기 II", "幼年期Ⅱ" },
        ["stage2"] = new[] { "Rookie", "Rookie", "Disciple", "Intermedio", "Novato", "Novato", "成長期", "성장기", "成長期" },
        ["stage3"] = new[] { "Champion", "Champion", "Champion", "Campione", "Campeón", "Campeão", "成熟期", "성숙기", "成熟期" },
        ["stage4"] = new[] { "Ultra", "Ultimate", "Ultime", "Evoluto", "Supremo", "Supremo", "完全体", "완전체", "完全體" },
        ["stage5"] = new[] { "Mega", "Mega", "Méga", "Mega", "Mega", "Mega", "究極体", "궁극체", "究極體" },
        ["stage6"] = new[] { "Mega+", "Mega+", "Méga+", "Mega+", "Mega+", "Mega+", "究極体+", "궁극체+", "究極體+" },

        // Setup
        ["setup.title"] = new[] { "Einrichtung", "Setup", "Installation", "Configurazione", "Configuración", "Configuração", "セットアップ", "설정", "設定" },
        ["setup.intro"] = new[] { "Damit die App deine Werte live sieht, kommt ein kleines Plugin ins Spiel. Dafür wird der Mod-Loader BepInEx (offizielle Version) in den Spielordner geladen.", "To read your values live, a small plugin is added to the game. This downloads the mod loader BepInEx (official build) into the game folder.", "Pour lire tes valeurs en direct, un petit plugin est ajouté au jeu. Le chargeur de mods BepInEx (version officielle) est téléchargé dans le dossier du jeu.", "Per leggere i valori in tempo reale viene aggiunto un piccolo plugin al gioco. Il mod loader BepInEx (versione ufficiale) viene scaricato nella cartella del gioco.", "Para leer tus valores en vivo se añade un pequeño plugin al juego. Se descarga el cargador de mods BepInEx (versión oficial) en la carpeta del juego.", "Para ler seus valores ao vivo, um pequeno plugin é adicionado ao jogo. O carregador de mods BepInEx (versão oficial) é baixado para a pasta do jogo.", "値をリアルタイムで読むため、小さなプラグインをゲームに追加します。MODローダー BepInEx（公式版）をゲームフォルダーにダウンロードします。", "실시간으로 값을 읽기 위해 작은 플러그인을 게임에 추가합니다. 모드 로더 BepInEx(공식 버전)를 게임 폴더에 내려받습니다.", "為了即時讀取數值，會在遊戲中加入一個小外掛，並把模組載入器 BepInEx（官方版本）下載到遊戲資料夾。" },
        ["setup.game"] = new[] { "Spiel", "Game", "Jeu", "Gioco", "Juego", "Jogo", "ゲーム", "게임", "遊戲" },
        ["setup.notfound"] = new[] { "nicht gefunden", "not found", "introuvable", "non trovato", "no encontrado", "não encontrado", "見つかりません", "찾을 수 없음", "找不到" },
        ["setup.installed"] = new[] { "installiert", "installed", "installé", "installato", "instalado", "instalado", "導入済み", "설치됨", "已安裝" },
        ["setup.missing"] = new[] { "fehlt", "missing", "manquant", "mancante", "falta", "ausente", "未導入", "없음", "未安裝" },
        ["setup.outdated"] = new[] { "veraltet", "outdated", "obsolète", "obsoleto", "desactualizado", "desatualizado", "古いバージョン", "구버전", "版本過舊" },
        ["setup.install"] = new[] { "Installieren", "Install", "Installer", "Installa", "Instalar", "Instalar", "インストール", "설치", "安裝" },
        ["setup.update"] = new[] { "Plugin aktualisieren", "Update plugin", "Mettre à jour le plugin", "Aggiorna plugin", "Actualizar plugin", "Atualizar plugin", "プラグインを更新", "플러그인 업데이트", "更新外掛" },
        ["setup.pick"] = new[] { "Spielordner wählen …", "Choose game folder …", "Choisir le dossier du jeu …", "Scegli cartella del gioco …", "Elegir carpeta del juego …", "Escolher pasta do jogo …", "ゲームフォルダーを選択…", "게임 폴더 선택…", "選擇遊戲資料夾…" },
        ["setup.remove"] = new[] { "Plugin entfernen", "Remove plugin", "Retirer le plugin", "Rimuovi plugin", "Quitar plugin", "Remover plugin", "プラグインを削除", "플러그인 제거", "移除外掛" },
        ["setup.close"] = new[] { "Fertig", "Done", "Terminé", "Fatto", "Listo", "Pronto", "閉じる", "완료", "完成" },
        ["setup.closegame"] = new[] { "Bitte zuerst das Spiel beenden.", "Please close the game first.", "Ferme d'abord le jeu.", "Chiudi prima il gioco.", "Cierra el juego primero.", "Feche o jogo primeiro.", "先にゲームを終了してください。", "먼저 게임을 종료하세요.", "請先關閉遊戲。" },
        ["setup.download"] = new[] { "Lade BepInEx herunter …", "Downloading BepInEx …", "Téléchargement de BepInEx …", "Download di BepInEx …", "Descargando BepInEx …", "Baixando BepInEx …", "BepInExをダウンロード中…", "BepInEx 다운로드 중…", "正在下載 BepInEx…" },
        ["setup.extract"] = new[] { "Entpacke in den Spielordner …", "Extracting into game folder …", "Extraction dans le dossier du jeu …", "Estrazione nella cartella del gioco …", "Extrayendo en la carpeta del juego …", "Extraindo na pasta do jogo …", "ゲームフォルダーに展開中…", "게임 폴더에 압축 해제 중…", "正在解壓縮到遊戲資料夾…" },
        ["setup.done"] = new[] { "Fertig! Starte jetzt das Spiel und lade deinen Spielstand. Der erste Start dauert 1–3 Minuten länger.", "Done! Start the game and load your save. The first start takes 1–3 minutes longer.", "Terminé ! Lance le jeu et charge ta sauvegarde. Le premier lancement prend 1 à 3 minutes de plus.", "Fatto! Avvia il gioco e carica il salvataggio. Il primo avvio richiede 1–3 minuti in più.", "¡Listo! Inicia el juego y carga tu partida. El primer inicio tarda 1–3 minutos más.", "Pronto! Inicie o jogo e carregue seu save. A primeira inicialização leva 1–3 minutos a mais.", "完了！ゲームを起動してセーブデータをロードしてください。初回起動は1～3分長くかかります。", "완료! 게임을 실행하고 세이브를 불러오세요. 첫 실행은 1~3분 더 걸립니다.", "完成！請啟動遊戲並讀取存檔。第一次啟動會多花 1–3 分鐘。" },
        ["setup.removed"] = new[] { "Plugin entfernt. Um BepInEx selbst zu entfernen, lösche im Spielordner den Ordner BepInEx und winhttp.dll.", "Plugin removed. To remove BepInEx itself, delete the BepInEx folder and winhttp.dll in the game folder.", "Plugin retiré. Pour retirer BepInEx, supprime le dossier BepInEx et winhttp.dll dans le dossier du jeu.", "Plugin rimosso. Per rimuovere BepInEx elimina la cartella BepInEx e winhttp.dll nella cartella del gioco.", "Plugin eliminado. Para quitar BepInEx, borra la carpeta BepInEx y winhttp.dll en la carpeta del juego.", "Plugin removido. Para remover o BepInEx, apague a pasta BepInEx e o winhttp.dll na pasta do jogo.", "プラグインを削除しました。BepInEx本体を消すには、ゲームフォルダーのBepInExフォルダーとwinhttp.dllを削除してください。", "플러그인을 제거했습니다. BepInEx를 지우려면 게임 폴더의 BepInEx 폴더와 winhttp.dll을 삭제하세요.", "已移除外掛。若要移除 BepInEx，請刪除遊戲資料夾中的 BepInEx 資料夾與 winhttp.dll。" },
        ["setup.failed"] = new[] { "Fehlgeschlagen: {0}", "Failed: {0}", "Échec : {0}", "Non riuscito: {0}", "Error: {0}", "Falhou: {0}", "失敗：{0}", "실패: {0}", "失敗：{0}" },
        ["setup.pickexe"] = new[] { "Digimon World Next Order.exe auswählen", "Select Digimon World Next Order.exe", "Sélectionne Digimon World Next Order.exe", "Seleziona Digimon World Next Order.exe", "Selecciona Digimon World Next Order.exe", "Selecione Digimon World Next Order.exe", "Digimon World Next Order.exe を選択", "Digimon World Next Order.exe 선택", "選擇 Digimon World Next Order.exe" },
    };
}
