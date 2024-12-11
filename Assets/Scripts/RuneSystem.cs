using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static public class RuneSystem
{
    static public string RuneAssign(int x, int y)
    {
        if (y <= 5 && y >= 2)
        {
            if ((y == 2 || y == 5) && (x == 0 || x == 2 || x == 5 || x == 7))
            {
                return "None";
            }
            //문양이 없는 타일을 제외하고 나머지 타일에 무작위로 문양을 할당
            else
            {
                int rand = Random.Range(1, 11);
                switch (rand)
                {
                    case 1: return "Paralyze";
                    case 2: return "Vines";
                    case 3: return "Frozen";
                    case 4: return "Haste";
                    case 5: return "Slow";
                    case 6: return "Smite";
                    case 7: return "Flash";
                    case 8: return "Observe";
                    case 9: return "Erase";
                    case 10: return "Choose";
                    default: return "None";
                }
            }
        }
        return "None";
    }

    static public void RuneActivate(int x, int y)
    {
        //밟은 문양의 이름을 확인
        string activeRune = GameObject.Find("ChessBoard").GetComponent<Chessboard>().tiles[x, y].GetComponent<Rune>().tileRune;
        GameObject ui = GameObject.Find("UI");
        switch (activeRune)
        {
            case "Paralyze": // 마비 (상대 기물 선택, 다음 차례 이동 불가 - 상대에게 적용)
                Debug.Log("Rune: Paralyze");
                paralyze();
                break;
            case "Vines": // 덩굴 (해당 tile 위치한 기물, 다음 턴 이동 불가 - 나한테 적용)
                Debug.Log("Rune:Vines");
                vines();
                break;
            case "Frozen": // 빙결 (이어지는 상대 turn, 무적 상태)
                Debug.Log("Frozen");
                frozen();
                break;
            case "Erase": // 소멸 (선택 tile 심볼 제거)
                Debug.Log("Rune:Erase");
                erase();
                break;
            case "Slow": // 감속 (다음 내 턴 타이머 +15s)
                Debug.Log("Rune:Slow");
                slow();
                break;
            case "Haste": // 가속 (다음 상대 턴 타이머 15s)
                Debug.Log("Rune:Haste");
                haste();
                break;
            case "Smite": // 강타 (무작위 방향으로 해당 기물 이동)
                Debug.Log("Rune:Smite");
                smite();
                break;
            case "Flash": // 점멸 (해당 타일 포함 주변 9개 tile 중 비어있는 곳으로 이동 가능)
                Debug.Log("Rune:Flash");
                flash();
                break;
            case "Observe": // 관측 (선택 tile 심볼 확인)
                Debug.Log("Rune:Observe");
                observe();
                break;
            default:    //밟은 문양이 None이면 문양 발동 페이즈를 종료하고 타이머를 리셋
                GameObject.Find("ChessBoard").GetComponent<Chessboard>().isRunePhase = false;
                ui.GetComponent<UI>().resetTimer(30);
                break;
        }
    }

    static private void observe()//관측
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();

        //현재 마우스가 위치한 타일의 상태를 확인
        RaycastHit info;
        Ray ray = chessboard.currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            Vector2Int hitPosition = chessboard.GetComponent<Chessboard>().LookUpTileIndex(info.transform.gameObject);  //현재 마우스가 위치한 타일의 좌표를 확인
            if (chessboard.currentHover == -Vector2Int.one) // 마우스가 게임판으로 들어오는 경우
                                                            //currentHover==-Vector2Int.one은 마우스의 마지막 위치가 게임판 외부였음을 의미함
            {
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 layer 변경 ("Tile" -> "Hover")
            }
            // tile A -> tile B hovering 경우 (hovering change)
            if (chessboard.currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                // 기존 "Highlight" or "Tile" layer로 각각 복구
                chessboard.tiles[chessboard.currentHover.x, chessboard.currentHover.y].layer = chessboard.ReturnToOriginalTile();
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }

            //클릭한 위치의 타일의 문양출력
            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                string checkedRune = chessboard.tiles[hitPosition.x, hitPosition.y].GetComponent<Rune>().tileRune; //문양을 확인

                //RunePhase 종료
                chessboard.isRunePhase = false;
                GameObject.Find("UI").GetComponent<UI>().displayRune(checkedRune);
                GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
            }


        }
    }

    static private void flash() //점멸
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();   //게임이 진행되는 Chessboard 호출
        Vector2Int currentPos = chessboard.lastMove;    //chessboard의 lastMove를 불러와 마지막으로 밟힌 문양의 위치를 currentPos로 저장

        //점멸 문양을 밟은 기물은 놓일 위치를 결정하기 위해 곧바로 다시 들어올려짐
        if (!chessboard.islifting)
        {
            chessboard.islifting = true;
            chessboard.currentlyDragging = chessboard.chessPieces[currentPos.x, currentPos.y];
            chessboard.islifting = chessboard.liftingPiece();
        }

        //점멸 문양을 밟았을 때, 이동할 수 있는 위치를 표시하기 위한 리스트
        List<Vector2Int> validPos = new List<Vector2Int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                Vector2Int tmp = new Vector2Int(currentPos.x - 1 + i, currentPos.y - 1 + j);
                if ((0 <= tmp.x && tmp.x <= 7) && (0 <= tmp.y && tmp.y <= 7))
                {
                    if (chessboard.chessPieces[tmp.x, tmp.y] == null || tmp == currentPos)
                    {
                        validPos.Add(tmp);
                        chessboard.tiles[tmp.x, tmp.y].layer = LayerMask.NameToLayer("Highlight");  //이동 가능한 타일은 highlight로 레이어 설정}
                    }
                }
            }
        }
        //유효한 타일 위치를 availableMoves에 저장
        chessboard.availableMoves = validPos;

        //raycast로 현재 마우스 위치에 따른 타일 상의 좌표 반환
        RaycastHit info;
        Ray ray = chessboard.currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Highlight")))
        {
            Vector2Int hitPosition = chessboard.GetComponent<Chessboard>().LookUpTileIndex(info.transform.gameObject);

            //레이어가 highlight로 설정된 타일을 클릭하는 경우
            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                if (chessboard.availableMoves.Contains(hitPosition))    //클릭한 타일이 이동 가능한 타일이면
                {
                    if (hitPosition != currentPos)//기존 위치에서 다른 위치로 이동하는 경우
                    {
                        chessboard.chessPieces[hitPosition.x, hitPosition.y] = chessboard.chessPieces[currentPos.x, currentPos.y];
                        chessboard.chessPieces[currentPos.x, currentPos.y] = null;
                        chessboard.PositionSinglePiece(hitPosition.x, hitPosition.y, false);
                        chessboard.lastMove.x = hitPosition.x;
                        chessboard.lastMove.y = hitPosition.y;
                        GameObject.Find("UI").GetComponent<UI>().displayRune(chessboard.tiles[hitPosition.x, hitPosition.y].GetComponent<Rune>().tileRune);
                    }


                    //이동 종료를 위한 후처리  
                    Debug.Log(string.Format("{0},{1} -> {2},{3}", currentPos.x, currentPos.y, hitPosition.x, hitPosition.y));   //디버깅용 로그
                    chessboard.moveList.Add(new Vector2Int[] { currentPos, hitPosition });  //이동기록을 저장
                    if (chessboard.CheckforCheckMate()) //이동 이후에 체크메이트 상황이 되는지 확인
                        chessboard.CheckMate(chessboard.chessPieces[hitPosition.x, hitPosition.y].team);
                    chessboard.RemoveHighlightTiles(); // highlight 제거
                    chessboard.islifting = chessboard.landingPiece(); // chessPiece landing (islifting = false로 변경)
                    chessboard.currentlyDragging = null; // 선택 말 해제

                    if (hitPosition == currentPos)//제자리로 이동하는 경우 RunePhase 반복을 막기 위해 isRunePhase를 false로 변경
                    {
                        chessboard.isRunePhase = false;
                    }

                    //RunePhase 종료를 위한 후처리
                    GameObject.Find("UI").GetComponent<UI>().resetTimer(30);    //다음 턴 타이머를 30으로 리셋
                }
            }
        }
    }


    static private void smite()//강타
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();   //게임 진행 중인 체스보드 불러오기

        Vector2Int currentPos = chessboard.lastMove;    //문양 발동을 위해 현재 위치를 포함하여 이동 가능한 타일을 확인
        int x, y;
        //현재 기물의 위치를 포함하여 주변 9칸 중 랜덤으로 타일을 선택
        x = currentPos.x + Random.Range(0, 3) - 1;
        y = currentPos.y + Random.Range(0, 3) - 1;

        //만약 선택된 타일이 게임판 범위를 넘어간다면 무효 처리
        if (x < 0 || x > 7) return;
        if (y < 0 || y > 7) return;

        //기존 위치에서 smite로 인해 이동될 위치를 표시
        Debug.Log(string.Format("Estimated Smite: {0},{1} -> {2},{3}", currentPos.x, currentPos.y, x, y));

        if (chessboard.chessPieces[x, y] == null) //선택된 타일에 아무 기물도 없을 경우에만 작동
        {
            //이동
            chessboard.chessPieces[x, y] = chessboard.chessPieces[currentPos.x, currentPos.y];
            chessboard.chessPieces[currentPos.x, currentPos.y] = null;
            chessboard.PositionSinglePiece(x, y, false);
            chessboard.lastMove.x = x;
            chessboard.lastMove.y = y;

            //실제로 이동된 좌표를 표시
            Debug.Log(string.Format("Real Smite:{0},{1} -> {2},{3}", currentPos.x, currentPos.y, x, y));

            //이동 종료를 위한 후처리
            chessboard.moveList.Add(new Vector2Int[] { currentPos, new Vector2Int(x, y) });
            if (chessboard.CheckforCheckMate())
                chessboard.CheckMate(chessboard.chessPieces[x, y].team);

            GameObject.Find("UI").GetComponent<UI>().displayRune(chessboard.tiles[x, y].GetComponent<Rune>().tileRune);
        }


        //RunePhase를 종료하고 타이머 재설정
        GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
    }

    static private void slow() // 감속 (다음 턴 +15s)
    {
        //게임이 진행 중인 Chessboard를 불러와 다음 자신의 턴에 bool isHaste를 확인하여 타이머에 15초를 더한다.
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();

        //RunePhase 종료
        chessboard.isRunePhase = false;
        GameObject.Find("UI").GetComponent<UI>().resetTimer(30); //resetTimer()에서 chessboard.isHaste를 확인하고 true면 15초가 추가된다.

        chessboard.isHaste = true;//상대 턴을 위한 타이머를 먼저 재설정하고 이후에 isHaste를 true로 변경하여 다음 자신의 턴에 문양 효과가 발동된다.
    }

    static private void haste() // 가속 (타이머 15s)
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();
        chessboard.isRunePhase = false;
        GameObject.Find("UI").GetComponent<UI>().resetTimer(15);   //타이머 시간을 15초로 재설정한다. 만약 isHaste가 true라면 여기에 15초를 더해 총 30초가 된다.
    }

    static private void erase()//소멸
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();

        //현재 마우스가 위치한 타일의 상태를 확인
        RaycastHit info;
        Ray ray = chessboard.currentCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            Vector2Int hitPosition = chessboard.GetComponent<Chessboard>().LookUpTileIndex(info.transform.gameObject);  //현재 마우스가 위치한 타일의 좌표를 확인
            if (chessboard.currentHover == -Vector2Int.one) // 마우스가 게임판으로 들어오는 경우
                                                            //currentHover==-Vector2Int.one은 마우스의 마지막 위치가 게임판 외부였음을 의미함
            {
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 layer 변경 ("Tile" -> "Hover")
            }
            // tile A -> tile B hovering 경우 (hovering change)
            if (chessboard.currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                // 기존 "Highlight" or "Tile" layer로 각각 복구
                chessboard.tiles[chessboard.currentHover.x, chessboard.currentHover.y].layer = chessboard.ReturnToOriginalTile();
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }

            //클릭한 위치의 타일의 문양을 None으로 변경
            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                chessboard.tiles[hitPosition.x, hitPosition.y].GetComponent<Rune>().tileRune = "None";//문양을 None으로 변경
                chessboard.tiles[hitPosition.x, hitPosition.y].transform.name = string.Format("X:{0} Y:{1} Rune:None", hitPosition.x, hitPosition.y);//유니티 하이어커리창에서의 타일의 이름을 변경

                //RunePhase 종료
                chessboard.isRunePhase = false;
                GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
            }


        }
    }

    static private void frozen()//빙결
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();
        Vector2Int lastMove = chessboard.lastMove;

        chessboard.chessPieces[lastMove.x, lastMove.y].isInvincible = true; //문양을 밟은 기물의 무적상태를 true로 변경
                                                                            //Chessboard.MoveTo()에서 공격받는 기물의 isInvincible이 true면 이동이 불가능한 판정
                                                                            //Chessboard.ActiveTeamPieces()에서 기물의 isInvincible을 false로 전환한다.

        //RunePhase 종료, 타이머 리셋
        chessboard.isRunePhase = false;
        GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
    }

    static private void vines()//덩굴
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();
        Vector2Int lastMove = chessboard.lastMove;

        chessboard.chessPieces[lastMove.x, lastMove.y].isActive = false;
        chessboard.chessPieces[lastMove.x, lastMove.y].isVined = true;
        //Chessboard.Update()에서 isActive=false인 기물은 선택이 불가능
        //Chessboard.ActiveTeamPieces()에서 isVined가 true면 false로 변경, 이후 isVined==false인 상태에서만 isActive를 true로 변경할 수 있도록 제한하여 돌아오는 자신의 턴에 기물 이동 제한

        chessboard.isRunePhase = false;
        GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
    }

    static private void paralyze() // 마비
    {
        Chessboard chessboard = GameObject.Find("ChessBoard").GetComponent<Chessboard>();
        RaycastHit info; // raycast가 충돌한 정보 저장
        Ray ray = chessboard.currentCamera.ScreenPointToRay(Input.mousePosition); // 마우스 위치에서 화면 상 좌표를 3D로 변환하는 ray 생성
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover", "Highlight", "Insight", "Special")))
        {
            Vector2Int hitPosition = chessboard.GetComponent<Chessboard>().LookUpTileIndex(info.transform.gameObject);  //클릭한 타일 좌표 저장
            if (chessboard.currentHover == -Vector2Int.one) // hovering background -> tile (new hovering)
            {
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // tile에 hover 효과 적용을 위해 layer 변경 ("Tile" -> "Hover")
            }
            // tile A -> tile B hovering 경우 (hovering change)
            if (chessboard.currentHover != hitPosition) // hovering tile A -> tile B (tile 변경)
            {
                // 기존 "Highlight" or "Tile" layer로 각각 복구
                chessboard.tiles[chessboard.currentHover.x, chessboard.currentHover.y].layer = chessboard.ReturnToOriginalTile();
                chessboard.currentHover = hitPosition;
                chessboard.tiles[hitPosition.x, hitPosition.y].layer = LayerMask.NameToLayer("Hover"); // 새로운 hover tile -> "Hover" layer로 변경
            }

            if (Input.GetMouseButtonDown(0)) // mouse left 클릭
            {
                // 첫번째 클릭에 관한 작업 (chess 말 선택)
                if (chessboard.chessPieces[hitPosition.x, hitPosition.y] != null && !chessboard.islifting) // 클릭한 위치에 chessPiece 존재 && chessPiece lifting X (none chosen chessPiece)
                {
                    if ((chessboard.chessPieces[hitPosition.x, hitPosition.y].team == 1 && !chessboard.isWhiteTurn && chessboard.currentTeam == 1)
                    || (chessboard.chessPieces[hitPosition.x, hitPosition.y].team == 0 && chessboard.isWhiteTurn && chessboard.currentTeam == 0)) // check turn
                    {
                        chessboard.currentlyDragging = chessboard.chessPieces[hitPosition.x, hitPosition.y]; // 현재 클릭 한 chess 말 저장
                        chessboard.HighlightTiles(); // highlight
                        chessboard.islifting = chessboard.liftingPiece(); // chessPiece lifting (islifting = true로 변경)
                    }
                }
                else if (chessboard.islifting) // islifting = chessPiece가 선택되어 있고 공중에 있음 (already chosen ChessPiece)
                {
                    Vector2Int previousPosition = new Vector2Int(chessboard.currentlyDragging.currentX, chessboard.currentlyDragging.currentY); // currentX, Y = 이동 전 위치

                    if (previousPosition == hitPosition)    //이미 떠있는 기물을 다시 클릭하여 마비 문양 대상 기물을 선택
                    {
                        chessboard.chessPieces[hitPosition.x, hitPosition.y].isActive = false;
                        chessboard.isRunePhase = false;
                        GameObject.Find("UI").GetComponent<UI>().resetTimer(30);
                        Debug.Log("RunPhase off");
                    }

                    //마비가 적용된 기물을 다시 게임판으로 내려놓음
                    chessboard.currentlyDragging.SetPosition(chessboard.GetTileCenter(previousPosition.x, previousPosition.y), false); // 위치 원상 복구
                    chessboard.RemoveHighlightTiles(); // highlight 제거
                    chessboard.islifting = chessboard.landingPiece(); // chessPiece landing (islifting = false로 변경)
                    chessboard.currentlyDragging = null; // 선택 말 해제
                }
            }
        }
        else // "Tile", "Hover", "Highlight" 객체와 충돌 X (board 아닌 곳 hover 하는 중)
        {
            // tile -> background
            if (chessboard.currentHover != -Vector2Int.one) // tile 작업 - chess board에서 벗어난 곳으로 hover 시
            {
                chessboard.tiles[chessboard.currentHover.x, chessboard.currentHover.y].layer = chessboard.ReturnToOriginalTile();
                chessboard.currentHover = -Vector2Int.one; // tile 밖이므로 위치 정보 (-1, -1) 저장
            }

            if (chessboard.currentlyDragging && Input.GetMouseButtonDown(0) && chessboard.islifting) // chess 말 작업 - chess board에서 벗어난 곳으로 drag 시
            {
                chessboard.currentlyDragging.SetPosition(chessboard.GetTileCenter(chessboard.currentlyDragging.currentX, chessboard.currentlyDragging.currentY), false); // 위치 원상 복구
                chessboard.RemoveHighlightTiles();
                chessboard.islifting = chessboard.landingPiece();
                chessboard.currentlyDragging = null;
            }
        }
    }
}