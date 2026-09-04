// ============================================
// 全功能測試劇本
// DialogSystem 的「ink 劇本」欄位換成 test_all.json 就能跑
//
// 這份只用「資料表裡真的有」的素材。第 1～9 段不該有任何警告；
// 第 10 段（重影／錯誤）程式端還沒實作，第 11 段是故意測錯誤處理的，這兩段會印警告。
//
// 目前資料表裡有的東西：
//   角色 saki（紗季）、momoka（桃花）
//   背景 saki_room_dark、saki_room_day、apartment_living、apartment_entrance
//   立繪 saki_normal、momoka_normal
//   特效字 rise
// ============================================

->t01_text


// ── 1. 基本文字與逐字顯示 ──────────────────
==t01_text==
@bg:apartment_living
@fade:in dur:0.5

#speaker:
測試 1／9：基本文字。

點擊或空白鍵推進。

打字途中再點一次會直接把整句顯示完，這句就是拿來測那個的，所以刻意寫長一點。

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

@bg:saki_room_day
現在是清晨的房間，直接切換，沒有過場。

@bg:saki_room_dark in:fade dur:1.5
剛剛應該淡出換成夜晚的房間再淡入，整段 1.5 秒。

@bg:apartment_entrance in:fade nowait
這句加了 nowait，所以文字跟淡入淡出同時進行。

@bg:apartment_living in:fade
換回客廳，這句會等它跑完才出現。
->t04_fade


// ── 4. 全螢幕淡入淡出 ──────────────────────
==t04_fade==
測試 4／9：淡入淡出。

@fade:out
@fade:in
剛剛應該黑了一下又亮回來，預設 0.5 秒。

@fade:out dur:1.5 color:white
@fade:in dur:1.5
這次是白色的，而且慢很多。

@fade:out nowait
這句加了 nowait，所以文字會在畫面變黑的同時出現。
@fade:in
恢復。
->t05_fx


// ── 5. 特效字 ──────────────────────────────
==t05_fx==
測試 5／9：特效字。

@fx:滴滴滴滴 anim:rise
剛剛應該有四個字依序飛出去。

@fx:好 anim:rise dur:0.4
這次只有一個字，而且比較快。

@fx:碰 anim:rise nowait
加了 nowait，所以文字不等特效字播完就出現。

@fade:out
@fx:滴滴滴滴 anim:rise
@fade:in
特效字畫在黑幕之上，所以剛剛在全黑的畫面裡也看得見。
->t06_sprite


// ── 6. 立繪 ────────────────────────────────
==t06_sprite==
測試 6／9：立繪。目前只有 normal 表情。

@sprite:momoka at:left
桃花從左邊淡入。

@sprite:saki at:right
紗季從右邊淡入。

#speaker:momoka
現在我在說話，我應該是亮的，紗季應該縮小變暗。

#speaker:saki
換我說話，這次應該反過來。

#speaker:
旁白。兩個人預設都維持原狀。

@sprite:momoka at:right
桃花移到右邊，跟紗季站同一個位置。

#speaker:momoka
同一個位置站兩個人時，說話的人會被推到最前面。

#speaker:saki
換我，我應該擠到前面，桃花被推到後面變小變暗。

#speaker:
@sprite:momoka out
桃花退場。

@sprite:saki at:center in:cut
紗季用 in:cut 直接出現在中間，沒有淡入。

@sprite:* out
全部退場。
->t07_clear


// ── 7. 清空對話框 ──────────────────────────
==t07_clear==
#speaker:
測試 7／9：清空。

#speaker:momoka
這句有名字，下一步只清文字、名字欄留著。

@clear:text
#speaker:momoka
名字欄應該還在。

@clear:name
#speaker:
名字欄應該不見了，文字還在。

@clear
兩個都清掉之後才顯示這句。
->t08_var


// ── 8. 變數與條件 ──────────────────────────
==t08_var==

->t09_dizzy


// ── 9. 暈眩 ────────────────────────────────
==t09_dizzy==
測試 9／10：暈眩。等待規則跟其他指令相反。

@dizzy:pulse strength:1 dur:1.2
剛剛應該暈了一下就恢復。pulse 預設會等它跑完。

@dizzy:on strength:0.3 dur:1.5
這句應該一邊漸暈一邊出現，因為 on 預設不等。

房間在慢慢地轉。

@dizzy:on strength:0.8 dur:3
越來越嚴重。

......

@dizzy:off dur:2
開始退。這句一樣不等，所以是邊退邊講。

@dizzy:off dur:0.5 wait
這句加了 wait，所以要等完全退乾淨才出現。
->t10_ghost_glitch


// ── 10. 重影與錯誤（尚未實作，會印警告）────
==t10_ghost_glitch==
測試 10／11：重影與錯誤。這兩個規格已定但程式端還沒做，所以這段會印警告。

@ghost:pulse
重影疊一下就恢復。

@ghost:on strength:0.4 dur:1.5
持續的重影，這句不等它漸強。

@ghost:on offset:20 count:3
三層殘影，每層偏移 20。

@ghost:off dur:1
退掉。

@glitch:pulse strength:1 dur:0.15
很短、很重的一下。

@dizzy:on strength:0.3 dur:1
@ghost:on strength:0.3 dur:1
暈眩和重影疊在一起，這是 SAN 值明顯下降的樣子。

@dizzy:off
@ghost:off
恢復正常。
->t11_error


// ── 11. 錯誤處理（這段會印警告，是故意的）─
==t11_error==
測試 11／11：故意寫錯，Console 應該出現警告但不會中斷。

@bg:這個背景不存在
背景應該維持原樣，不會變黑。

#speaker:這個角色不存在
名字欄應該直接顯示 key 本身。

#speaker:
@sprite:這個角色不存在 at:left
立繪應該沒有出現。

@fx:測試 anim:這個動畫不存在
特效字應該用資料表第一筆的設定。

@這個指令不存在:值
不認得的指令只印警告。

測試結束。
@fade:out
->END
