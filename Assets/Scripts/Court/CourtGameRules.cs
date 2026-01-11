using UnityEngine;
using System.Collections.Generic;

// [DeckManager의 데이터 타입을 사용하기 위한 네임스페이스 확인 필요]
// CardValue, TierEnum 등이 전역 namespace에 있다면 그대로 사용.

namespace Court
{
    public static class CourtGameRules
    {
        // ==================================================================================
        // ★ 1. 공개 API (Presenter와 Model이 사용할 함수들)
        // ==================================================================================

        /// <summary>
        /// (서버용) 실제 점수 변동량을 계산합니다. (N카드일 경우 Random 확률 적용)
        /// </summary>
        public static int CalculateFinalScore(CardItemData card1, CardItemData card2, int currentTargetVote)
        {
            // 1. 역할 분담 (누가 기호고 누가 숫자인지)
            ClassifyCards(card1, card2, out CardItemData opCard, out CardItemData valCard, out bool isNInteraction);

            int numberValue = 0;
            
            // 2. 숫자 결정 (N이면 확률, 일반이면 고정값)
            if (isNInteraction)
            {
                numberValue = ResolveNCardValue(opCard); // 기호 등급에 따라 랜덤 결정
            }
            else
            {
                numberValue = GetNumberValue(valCard);
            }

            // 3. 연산 수행 (기호와 숫자로 최종 변동량 계산)
            string op = GetOperatorType(opCard);
            return CalculateDelta(currentTargetVote, op, numberValue);
        }

        /// <summary>
        /// (클라이언트 프리뷰용) N 카드가 포함된 조합인지 확인합니다.
        /// </summary>
        public static bool IsUnknownResult(CardItemData card1, CardItemData card2)
        {
            return IsNCard(card1) || IsNCard(card2);
        }

        /// <summary>
        /// (클라이언트 프리뷰용) 일반 카드 조합일 때 예측 점수(변동량)를 계산합니다.
        /// </summary>
        public static int CalculatePreviewScore(CardItemData card1, CardItemData card2, int currentTargetVote)
        {
            if (IsUnknownResult(card1, card2)) return 0; // N카드는 계산 불가

            return CalculateFinalScore(card1, card2, currentTargetVote);
        }

        /// <summary>
        /// 두 카드가 호환되는지 검사 (기존 로직 유지)
        /// </summary>
        public static bool IsCompatible(CardItemData card1, CardItemData card2)
        {
            bool hasOp = IsOperatorCard(card1) || IsOperatorCard(card2);
            bool hasNum = IsNumberCard(card1) || IsNumberCard(card2);
            bool hasN = IsNCard(card1) || IsNCard(card2);

            // 기호 + 숫자  OR  기호 + N  조합만 가능
            return hasOp && (hasNum || hasN);
        }

        // ==================================================================================
        // ★ 2. 내부 로직 (규칙 구현부)
        // ==================================================================================

        private static void ClassifyCards(CardItemData c1, CardItemData c2, out CardItemData opCard, out CardItemData valCard, out bool isN)
        {
            if (IsOperatorCard(c1)) { opCard = c1; valCard = c2; }
            else { opCard = c2; valCard = c1; }
            
            isN = IsNCard(valCard);
        }

        // --- 값 식별 헬퍼 (DeckManager Enums 사용) ---
        private static bool IsOperatorCard(CardItemData card) => card.cardDef.type == TypeEnum.Operator;
        private static bool IsNumberCard(CardItemData card) => card.cardDef.type == TypeEnum.Number;
        private static bool IsNCard(CardItemData card) => card.cardDef.Value == CardValue.N; // 혹은 SubTypeEnum.N

        private static int GetNumberValue(CardItemData card)
        {
            if (card.cardDef.Value >= CardValue.V0 && card.cardDef.Value <= CardValue.V6)
                return (int)card.cardDef.Value;
            return 0;
        }

        private static string GetOperatorType(CardItemData card)
        {
            switch (card.cardDef.Value)
            {
                case CardValue.ADD: return "+";
                case CardValue.SUB: return "-";
                case CardValue.MULT: return "x";
                case CardValue.DIV: return "/";
                default: return "+";
            }
        }

        // --- 확률 로직 (N 카드) ---
        private static int ResolveNCardValue(CardItemData opCard)
        {
            // 기호 카드의 등급(Tier)에 따라 범위 결정
            // Common=Bronze, Rare=Silver, Special=Gold
            switch (opCard.cardDef.tier)
            {
                case TierEnum.Special: // Gold
                    return Random.Range(0, 7); // 0~6
                
                case TierEnum.Rare:    // Silver
                    return Random.Range(1, 5); // 1~4
                
                case TierEnum.Common:  // Bronze
                default:
                    return Random.Range(1, 3); // 1~2
            }
        }

        // --- 최종 연산 로직 (Delta 계산) ---
        private static int CalculateDelta(int current, string op, int val)
        {
            int finalResult = current;

            switch (op)
            {
                case "+": finalResult = current + val; break;
                case "-": finalResult = current - val; break;
                
                case "x": 
                case "*":
                    if (val == 0) finalResult = 0; // x0 무효화
                    else finalResult = current * val;
                    break;
                
                case "/": 
                case "÷":
                    if (val == 0) finalResult = current + 10; // ÷0 패널티
                    else finalResult = current / val;
                    break;
            }

            return finalResult - current; // 변동량 반환
        }
    }
}