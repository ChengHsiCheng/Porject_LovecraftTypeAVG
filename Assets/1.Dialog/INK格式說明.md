# ink 劇本格式說明

古神咖啡廳｜對話系統劇本撰寫規範
最後更新：2026-09-04

---

## 支援狀況

| 功能 | 狀況 |
|---|---|
| 一行一句、逐字顯示、點擊推進、Ctrl 快轉 | ✅ |
| knot 分段與跳轉、變數、條件、內插 | ✅ |
| `@bg` `@fade` `@fx` `@sprite` `@clear` | ✅ |
| `@dizzy` `@ghost` `@glitch` `@dream` `@dark` | ✅ |
| `#speaker` | ✅ |
| `@se` `@bgm` `@ambient` `@wait` `@card` `#speed` … | ⚠️ 只印警告，沒有效果 |
| 選項（`*` `+`） | ❌ 寫了會卡住 |
| EXTERNAL 函式 | ❌ 執行時報錯 |

---

## 1. 檔案

- `.ink` 存檔後切回 Unity 自動編譯出同名 `.json`
- `DialogSystem` 的欄位要拖 **`.json`**，不是 `.ink`
- 多檔用 `INCLUDE 檔名.ink` 從主檔串起來，只編譯主檔

## 2. 基本寫法

```ink
// 註解不會進遊戲

->prologue_dream        // 入口

==prologue_dream==      // 段落（knot）
睜開眼
熟悉的房間
->prologue_wake         // 跳段

==prologue_wake==
鬧鐘響了
->END
```

- **一行 = 一句 = 一次點擊**，空行不產生內容
- ⚠️ **knot 名稱只能用英數與底線，不能有中文或空格**（會編譯失敗）。格式 `檔名_英文用途`

---

## 3. 演出指示：`@` 和 `#`

**`@` 是「在這裡做一件事」，`#` 是「描述下面那句話」。**

| | `@` 指令行 | `#` tag |
|---|---|---|
| 寫在哪 | 自己單獨一行 | 它要修飾的那句文字**上方** |
| 執行時機 | 就在寫的位置，由上往下 | 跟**下一句文字同時**發生 |
| 需要文字嗎 | 不用 | **一定要有下一句文字**，否則不執行 |
| 放段落結尾 | ✅ | ❌ 不會執行 |
| 打錯字 | 被當台詞顯示出來 | 靜靜地不生效 |
| 目前有 | 除了 `#speaker` 以外全部 | 只有 `#speaker` |

判準：**這件事需不需要一句台詞才成立？** 需要 → `#`，不需要 → `@`。

### 寫法規則

```
@指令:值 選項:值 旗標
```

- **值**＝主參數（背景 key、要顯示的文字…），可省略
- **選項**＝用空白隔開的 `名稱:值`，順序不拘
- **旗標**＝沒有冒號的選項，寫了就開啟（`nowait`）
- 值裡有空白要用雙引號：`@fx:"滴滴 滴滴" anim:rise`
- 一行只寫一個指令
- 寫錯邊或不認得的指令只印警告，不會中斷遊戲

### 等待機制

會動的指令**預設等它跑完**才顯示文字，加 `nowait` 不等（想讓兩個效果同時發生就靠這個）。

⚠️ **五個畫面效果例外**：`on`／`off` 是持續狀態，預設**不等**（要邊暈邊講話），要等就寫 `wait`；只有 `pulse` 預設會等。

---

## 4. `@` 指令

### `@bg` 換背景

```ink
@bg:saki_room_dark              直接切換
@bg:cafe_night in:fade          淡出換圖再淡入
@bg:cafe_night in:fade dur:2    整段 2 秒（前後各半）
@bg:cafe_night in:fade nowait   不等它跑完
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | 背景 key | 必填 | 找不到會印警告並**維持原背景**，不會變黑 |
| `in:` | `cut` / `fade` | `cut` | `fade` 是淡出→換圖→淡入，不是交叉溶接 |
| `dur:` | 秒數 | 0.5 | 整段總時間 |
| `nowait` | 旗標 | 會等 | — |

### `@fade` 全螢幕淡入淡出

```ink
@fade:out               淡出（畫面被蓋住）
@fade:in                淡入（恢復）
@fade:out dur:2 color:white
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | `out` / `in` | 必填 | 填別的會印警告並跳過 |
| `dur:` | 秒數 | 0.5 | — |
| `color:` | `black` `white` `red` `#RRGGBB` | 黑 | 只有 `out` 需要 |
| `nowait` | 旗標 | 會等 | — |

⚠️ 黑幕**蓋住對話框與背景，但蓋不住特效字**。
⚠️ **`@fade:out` 之後一定要有 `@fade:in`**，忘了畫面會一直黑著，像當機。

### `@fx` 特效字（擬聲字）

文字寫在指令裡，不佔對話框、不需點擊。**預設一個字一個字分開飛。**

```ink
@fx:滴滴滴滴 anim:rise
@fx:碰 anim:rise nowait
@fx:"滴滴 滴滴" anim:rise
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | 要顯示的文字 | 必填 | 含空白要用 `"` 包起來 |
| `anim:` | 預設名稱 | 資料表第一筆 | 登記在 `FxTextDatabase` |
| `dur:` | 秒數 | 預設值 | **每個字**的長度，不是整串 |
| `nowait` | 旗標 | 會等 | 連丟好幾組時要加 |

動畫參數（字級、位移、曲線、逐字間隔、顏色…）在 `FxTextDatabase` 上調，加一筆就多一個 `anim:` 可用。

### `@sprite` 立繪

```ink
@sprite:momoka at:left              站左邊（表情 normal）
@sprite:momoka face:smile           只換表情
@sprite:momoka at:right             只換位置
@sprite:momoka out                  退場
@sprite:* out                       全部退場
@sprite:saki at:right in:cut        不淡入
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | 角色 key 或 `*` | 必填 | `*` 只能配 `out` |
| `face:` | 表情 | `normal` | 查表用 `角色_表情`，例 `momoka_smile` |
| `at:` | 位置名稱 | 第一次＝第一個錨點，之後＝原位 | `left` `center` `right` |
| `in:` | `fade` / `cut` | `fade` | 第一次出現的進場方式 |
| `out` | 旗標或 `out:cut` | — | 寫了就是退場 |
| `dur:` | 秒數 | 0.3 | — |
| `nowait` | 旗標 | 會等 | — |

**一個角色只有一份立繪**，重複下指令是換表情或換位置。

**自動行為（不用寫）**：
- `#speaker:` 一改 → 說話者維持原本大小亮度，**其他人縮小壓暗**
- 同一個 `at:` 站多人 → **說話者被推到最前**，其他人依序往後擠（更小、更暗、稍微偏移）
- 旁白時預設維持原狀

### 五個畫面效果：`@dizzy` `@ghost` `@glitch` `@dream` `@dark`

**共用同一套語法**：

```ink
@dizzy:on                        開始（預設強度）
@dizzy:on strength:0.8 dur:2     2 秒漸強到 0.8
@dizzy:off dur:3                 慢慢退
@dizzy:pulse strength:1 dur:1.2  來一下就恢復
@dizzy:on strength:0.4 wait      要等漸強跑完才顯示下一句
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | `on` / `off` / `pulse` | 必填 | `pulse` 是衝上去再退回 0 |
| `strength:` | 0～1 | 0.5 | 一個數值控制該效果的全部參數 |
| `dur:` | 秒數 | 1（`@glitch` 是 0.2） | 漸強／漸退／脈動時間 |
| `wait` | 旗標 | 不等 | `on`／`off` 專用 |
| `nowait` | 旗標 | 會等 | `pulse` 專用 |

| 指令 | 效果 | 用在哪 |
|---|---|---|
| `@dizzy` | 波紋扭曲＋旋轉＋色散＋壓黑 | 暈眩、站不穩 |
| `@ghost` | 疊出偏移的殘影 | 視線對不準焦、看成兩個 |
| `@glitch` | 橫條錯位＋RGB 分離＋雜訊 | 訊號故障、世界壞了 |
| `@dream` | 柔焦＋光暈＋褪色＋呼吸 | 夢境、回想、意識不清 |
| `@dark` | 單純壓暗 | 氣氛壓低、燈光轉暗 |

**五個可以疊加**，各自獨立：

```ink
@dizzy:on strength:0.3 dur:1
@ghost:on strength:0.3 dur:1
視野開始晃，而且看見了兩層。
```

**`@ghost` 額外參數**：

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| `offset:` | `20` 或 `20,6` | `12,0` | 殘影偏移。一個值＝XY 相同，兩個值＝X、Y 分別 |
| `count:` | 1～4 | 1 | 疊幾層 |

⚠️ `offset:` `count:` 會**改掉 Volume 上的值並留著**，之後沒指定的 `@ghost` 會沿用。

⚠️ **`@dark` 跟 `@fade:out` 的差別**：

| | 做什麼 | 對話框 | 用在哪 |
|---|---|---|---|
| `@fade:out` | 蓋一層不透明色塊 | 被蓋住 | 轉場、換場景 |
| `@dark:on` | 畫面本身變暗 | 一起變暗但**看得到** | **要繼續講話**的時候 |

各效果「強度 1 時」的細部參數在 Volume Profile 的對應 override 上調，劇本只給 `strength`。

### `@clear` 清空對話框

```ink
@clear          清文字 ＋ 隱藏名字欄
@clear:text     只清文字
@clear:name     只隱藏名字欄
```

不清的話上一句會留在框裡。轉場、特效字、黑畫面之前通常都要清。

### 標準轉場寫法

```ink
轉身衝向房門，伸手抓向金屬門把——
@clear                      ← 先清掉上一句
@fade:out dur:0.2           ← 淡黑
@fx:滴滴滴滴 anim:rise       ← 黑畫面裡的鬧鐘

@bg:saki_room_day           ← 畫面還是黑的，直接切就好
@fade:in dur:1
睜開眼。
```

**黑幕底下換背景用直接切**，疊兩層淡入淡出只是浪費時間。

### 之後會開的 `@` 指令（先寫沒關係，只印警告）

| 指令 | 用途 |
|---|---|
| `@se:doorbell` | 一次性音效 |
| `@ambient:clock_tick` | 環境音 loop（會自己停的用 `se`，要手動停的用 `ambient`） |
| `@bgm:calm` | 背景音樂 |
| `@wait:0.8` | 停頓，不必點擊 |
| `@card:tarot_01` | 圖卡 |
| `@ui:off` | 隱藏／顯示對話框 |
| `@minigame:service` | 進小遊戲 |

---

## 5. `#` tag

### `#speaker` 說話者

```ink
#speaker:momoka             名字欄顯示「桃花」
#speaker:saki as:？？？       顯示「？？？」（身分未明）
#speaker:                   隱藏名字欄（旁白）
```

| 參數 | 可填 | 預設 | 說明 |
|---|---|---|---|
| **值** | 角色 key | 空白＝旁白 | 找不到會印警告並**直接顯示 key 本身** |
| `as:` | 任意文字 | 不覆蓋 | 蓋掉資料表裡的顯示名 |

- **說話者沿用到下一個 `#speaker:` 為止**，同一人連講三句只寫一次
- **換成旁白一定要寫空的 `#speaker:`**，否則名字欄還掛著上一個人

### 之後會開的 `#` tag

`#speed:0.1`（這句的打字速度）、`#instant`（不逐字）、`#voice:xxx`（語音）、`#style:inner`（心聲樣式）

---

## 6. 共用參數與禁區

| 參數 | 意思 | 用在 |
|---|---|---|
| `dur:` | 秒數 | 幾乎全部 |
| `nowait` / `wait` | 不等／要等 | 會動的指令 |
| `in:` `out:` | 進場／退場方式 | `@bg` `@sprite` |
| `strength:` | 效果強度 0～1 | 五個畫面效果 |
| `color:` | 顏色 | `@fade` |
| `anim:` | 動畫預設名稱 | `@fx` |
| `at:` `face:` | 位置／表情 | `@sprite` |
| `as:` | 覆蓋顯示文字 | `#speaker` |
| `*` | 全部 | `@sprite:* out` |

**改變畫面與聲音才用 `@` / `#`；改變遊戲狀態一律用 ink 語法**（`~ flag_x = true`、`{條件:}`、`->knot`）。混在一起之後很難查問題。

---

## 7. 命名規範

**全小寫、英數與底線**，不用中文；key 不帶類別前綴（`cafe_night` 不是 `bg_cafe_night`）；**檔名＝前綴＋key**（`bg_cafe_night.png`）。

| 類型 | 格式 | 例 |
|---|---|---|
| 角色 | 本名羅馬拼音 | `momoka`（桃花／女A）、`saki`（紗季／女B）、`rena`（苓）、淵未定 |
| 案件角色 | `case編號_稱呼` | `case01_woman` |
| 背景 | `地點_時段` 或 `地點_狀態` | `cafe_night`、`saki_room_dark` |
| 立繪 | `角色_表情` | `momoka_smile` |
| 音效 | 直述 | `doorbell`、`clock_tick`、`calm` |
| 特效字動畫 | 動作 | `rise`、`pop`、`shake` |
| 圖卡 | `類別_編號` | `tarot_01` |
| ink 變數 | 前綴分類 | `flag_` `count_` `rel_` `ch01_` |

**固定字彙**（不要自己造同義詞，要加就寫進這張表）：

- 時段：`morning` `day` `evening` `night`｜狀態：`dark` `rain` `closed` `empty`
- 表情：`normal` `smile` `sad` `angry` `surprised` `tired` `serious`
- 位置：`left` `center` `right`

**檔案位置**：`2.Art/BG`（`bg_`）、`2.Art/Characters`（`ch_`）、`3.Audio/{BGM,SE,Ambient}`、`1.Dialog`（劇本）、`SO`（資料表）

---

## 8. 可用的進階語法

```ink
VAR 客人數 = 0
~ 客人數 = 3                              // 賦值

今天來了 {客人數} 個客人。                  // 內插
{客人數 > 0: 比昨天好一點。|一個都沒有。}    // 行內條件

{ 客人數 > 5:
    今天忙到沒空講話。
- else:
    閒得發慌。
}

{一次性內容|第二次會看到|之後都是這句}
&輪替一|輪替二|輪替三
```

也可以用 `=子段落`、tunnel（`->某段->`）、`== function 名稱() ==`、`LIST`。

---

## 9. 寫作注意

- **一句約 40 個中文字以內**，目前不會自動分頁
- 標點用全形。**刪節號例外——用 `......`（六個半形點）**
- 句尾不要留空格，會被算進顯示字元
- 中英文之間不用手動加空格

---

## 10. 範本

```ink
// ============================
// 第一章｜店裡的一天
// ============================

->ch01_open

==ch01_open==
@bg:cafe_night
@fade:in

#speaker:
店裡的燈只開了一半。

#speaker:momoka
今天也沒什麼客人。
#speaker:saki
妳從剛剛就一直趴在那裡。

#speaker:
門口的風鈴響了，但沒有人走進來。
->ch01_close

==ch01_close==
外面的天色暗得比昨天早。
@clear
@fade:out
@fx:叮鈴 anim:rise

@bg:cafe_closed
@fade:in
#speaker:saki
今天的營業還沒結束。
->END
```

---

## 11. 之後的實作順序

1. `@se` `@ambient` `@bgm` 音效音樂
2. `@wait`、`#speed`、`#instant`
3. 選項
4. `@card` 圖卡（等玩法定案）
5. 存讀檔
6. `@minigame` ＋ EXTERNAL 函式

**每加一項，這份文件同步更新。**

---

## 12. 本地劇本 ↔ ink 對應表

劇本正本在 `F:/Claude/古神咖啡廳-劇情/notion-mirror/序章劇本.md`。那份給人看（也同步到 Notion），所以**演出一律寫成自然語言註解**；這份 `.ink` 是從那邊轉出來的執行檔。

**一律從本地轉到 ink，不要反過來。** ink 是產出物，直接改它下次轉換會被蓋掉。

| 本地劇本 | ink |
|---|---|
| `// 背景：紗季的房間・夜` | `@bg:saki_room_dark` |
| `// 背景：公寓客廳（淡出換圖再淡入）` | `@bg:apartment_living in:fade` |
| `// 淡入，2 秒` | `@fade:in dur:2` |
| `// 特效字：滴滴滴滴（rise）` | `@fx:滴滴滴滴 anim:rise` |
| `// 立繪：桃花，右` | `@sprite:momoka at:right` |
| `// 立繪：全部退場` | `@sprite:* out` |
| `// 清空對話框` | `@clear` |
| `桃花：台詞` | `#speaker:momoka` ＋下一行台詞 |
| `苓（女僕）：台詞` | `#speaker:rena as:女僕` ＋下一行台詞 |
| 沒有前綴的敘述 | `#speaker:` ＋下一行敘述 |

**背景鍵值**：紗季的房間・夜 `saki_room_dark`／清晨 `saki_room_day`、公寓客廳 `apartment_living`、公寓玄關 `apartment_entrance`、街上・清晨 `street_morning`／傍晚 `street_evening`、大學教室・上午 `classroom_day`、咖啡廳門口・傍晚 `cafe_front_evening`

**還沒實作的演出兩邊都維持自然語言註解**，寫成指令會在 Console 洗版：

| 自然語言 | 之後對應到 |
|---|---|
| `// 音效：關門` | `@se:door` |
| `// 環境音：時鐘滴答，整段 loop` | `@ambient:clock_tick` |
| `// 效果：暈眩` | `@dizzy:pulse` |
