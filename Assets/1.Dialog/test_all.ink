// ============================================
// 全功能測試劇本
// 把 DialogSystem 的「ink 劇本」欄位換成 test_all.json 就能跑
//
// 需要先在資料表登記這些 key：
//   DialogDatabase 角色： momoka、saki
//   DialogDatabase 背景： cafe_day、cafe_night
//   DialogDatabase 立繪： momoka_normal、momoka_smile、saki_normal
//   FxTextDatabase 動畫： rise
// 沒登記的會印警告並跳過，其他測試照跑
// ============================================

VAR test_count = 0

->t01_text

// ── 1. 基本文字與逐字顯示 ──────────────────
==t01_text==
#speaker:
測試 1／9：基本文字。點擊或空白鍵推進。

打字途中再點一次，會直接把整句顯示完，這句就是拿來測那個的，所以刻意寫得長一點。

按住 Ctrl 可以連續快轉。
->t02_speaker

// ── 2. 說話者名字欄 ────────────────────────
==t02_speaker==
#speaker:
測試 2／9：名字欄。這句沒有名字。

#speaker:momoka
這句應該顯示「桃花」。
名字會沿用，所以這句也還是桃花。

#speaker:saki as:？？？
這句應該顯示「？？？」，不是紗季。

#speaker:saki
這句才顯示「紗季」。

#speaker:
回到旁白，名字欄應該不見了。
->t03_bg

// ── 3. 背景切換 ────────────────────────────
==t03_bg==
測試 3／9：背景。

@bg:cafe_day
現在是白天的店裡（直接切換）。

@bg:cafe_night in:fade dur:1.5
剛剛應該淡出換成夜晚再淡入，整段 1.5 秒。

@bg:cafe_day in:fade nowait
這句加了 nowait，所以文字跟淡入淡出同時進行。
->t04_fade

// ── 4. 全螢幕淡入淡出 ──────────────────────
==t04_fade==
測試 4／9：淡入淡出。

@clear
@fade:out dur:0.6
@fade:in dur:0.6
剛剛畫面應該黑了一下又回來。

@clear
@fade:out color:white dur:0.4
@fade:in dur:0.8
這次是白色閃一下。
->t05_fx

// ── 5. 特效字 ──────────────────────────────
==t05_fx==
測試 5／9：特效字。

@fx:滴滴滴滴 anim:rise
上面應該一個字一個字往上飛。

@clear
@fade:out dur:0.3
@fx:滴滴滴滴 anim:rise
黑畫面裡也應該看得到特效字（這句在特效字之後才出現）。

@fade:in dur:0.5
畫面回來了。

@fx:碰 anim:rise nowait
這句有加 nowait，所以文字跟特效字同時出現。

@fx:"叮 鈴" anim:rise
含空白的文字要用雙引號包起來。
->t06_sprite

// ── 6. 立繪：出現、表情、位置 ──────────────
==t06_sprite==
測試 6／9：立繪。

@sprite:momoka at:left
桃花從左邊淡入。

@sprite:momoka face:smile
只換表情，位置不動。

@sprite:momoka at:right
只換位置，表情維持 smile。

@sprite:momoka at:left in:cut
換回左邊，這次沒有淡入。
->t07_highlight

// ── 7. 說話者強調 ──────────────────────────
==t07_highlight==
#speaker:
測試 7／9：說話者強調。紗季要進場了。

@sprite:saki at:right
兩個人分別站左右。

#speaker:momoka
我在說話，我應該是亮的，紗季應該縮小壓暗。

#speaker:saki
換我說話，現在應該反過來。

#speaker:
旁白。預設維持上一句的亮暗（可在元件上改成全部壓暗）。
->t08_depth

// ── 8. 同位置多人的前後 ────────────────────
==t08_depth==
測試 8／9：同一個位置站兩個人。

@sprite:saki at:left
紗季移到左邊，跟桃花同一個位置。

#speaker:momoka
我在說話，我應該在前面，紗季被擠到後面（更小、更暗、偏移）。

#speaker:saki
換我說話，我應該被推到最前面。

#speaker:momoka
再換回來，兩人應該再對調一次。
->t09_ink

// ── 9. ink 語法與清空 ──────────────────────
==t09_ink==
#speaker:
測試 9／9：ink 語法。

~ test_count = 9
變數內插：目前是第 {test_count} 個測試。

{test_count > 5: 條件判斷：大於 5，這句應該出現。|不該看到這句。}

{這句只會出現一次|重複進來才會看到這句}

@clear
@sprite:* out
所有立繪應該淡出了，名字欄也清空了。
->t_end

==t_end==
@bg:cafe_night in:fade
@fade:out dur:0.5
@fx:測試結束 anim:rise
@fade:in dur:0.5
全部測試完畢。Console 裡的警告表示有 key 還沒登記。
->END
