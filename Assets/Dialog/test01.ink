EXTERNAL SetInterfere (value)
EXTERNAL CheckUsedItem (itemName)

->start

==start==
準備
~SetInterfere (true)
開始介入
1
2
3
~SetInterfere (false)
結束介入
測試技能 : 火球
{ CheckUsedItem("FireBall") :
        使用了火球
    - else: 
        沒用火球
}
測試技能 : 冰球
{ CheckUsedItem("IceBall") :
        使用了火球
    - else: 
        沒用火球
}

->END