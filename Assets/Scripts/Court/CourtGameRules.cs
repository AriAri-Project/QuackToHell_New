using UnityEngine;

namespace Court
{
    public static class CourtGameRules
    {
        public static bool IsCompatible(CardItemData cardA, CardItemData cardB)
        {
            // [수정] DeckManager에게 다시 물어볼 필요 없이, 이미 있는 데이터를 씁니다.
            // (CardItemData 구조체 안에 CardDef가 이미 들어있기 때문)
            CardDef defA = cardA.cardDef;
            CardDef defB = cardB.cardDef;

            // [디버그 로그] 확실한 확인을 위해 출력
            Debug.Log($"[Rule Log] 비교: A[{defA.type}/{defA.tier}/{defA.Value}] vs B[{defB.type}/{defB.tier}/{defB.Value}]");

            // 1. 타입 검사 (숫자 1개 + 연산자 1개 필수)
            bool hasNumber = defA.type == TypeEnum.Number || defB.type == TypeEnum.Number;
            bool hasOperator = defA.type == TypeEnum.Operator || defB.type == TypeEnum.Operator;

            if (!hasNumber || !hasOperator) 
            {
                Debug.LogError($"[Rule Log] ❌ 타입 조합 탈락! (A:{defA.type}, B:{defB.type}) -> 숫자+기호여야 함");
                return false; 
            }

            // 역할 분담
            CardDef numberDef = (defA.type == TypeEnum.Number) ? defA : defB;
            CardDef operatorDef = (defA.type == TypeEnum.Operator) ? defA : defB;

            // 2. 특수 카드 ('N') 처리 -> 무조건 통과
            if (numberDef.subType == SubTypeEnum.N || numberDef.Value == CardValue.N) 
            {
                return true; 
            }

            // 3. 티어별 숫자 범위 검사
            int numVal = GetIntFromCardValue(numberDef.Value);
            
            // 값이 이상하면 실패
            if (numVal == -1)
            {
                 Debug.LogError($"[Rule Log] ❌ 알 수 없는 숫자 값입니다: {numberDef.Value}");
                 return false;
            }

            switch (operatorDef.tier)
            {
                case TierEnum.Common: // 동색: 1 ~ 2만 가능
                    if (numVal >= 1 && numVal <= 2) return true;
                    Debug.LogError($"[Rule Log] ❌ Common(동) 연산자는 1, 2만 가능. (현재: {numVal})");
                    return false;

                case TierEnum.Rare:   // 은색: 1 ~ 4만 가능
                    if (numVal >= 1 && numVal <= 4) return true;
                    Debug.LogError($"[Rule Log] ❌ Rare(은) 연산자는 1~4만 가능. (현재: {numVal})");
                    return false;

                case TierEnum.Special:// 금색: 0 ~ 6 모두 가능
                    if (numVal >= 0 && numVal <= 6) return true;
                    Debug.LogError($"[Rule Log] ❌ Special(금) 범위 오류. (현재: {numVal})");
                    return false;

                default:
                    Debug.LogError($"[Rule Log] ❌ 알 수 없는 티어: {operatorDef.tier}");
                    return false;
            }
        }

        public static int CalculateEvidenceScore(CardItemData cardA, CardItemData cardB)
        {
            // 여기도 똑같이 수정: DeckManager 조회 제거
            CardDef defA = cardA.cardDef;
            CardDef defB = cardB.cardDef;

            var numDef = (defA.type == TypeEnum.Number) ? defA : defB;
            var opDef = (defA.type == TypeEnum.Operator) ? defA : defB;

            int baseValue = GetIntFromCardValue(numDef.Value);
            if (baseValue < 0) baseValue = 0; 

            int multiplier = 1;
            switch (opDef.tier)
            {
                case TierEnum.Common:  multiplier = 1; break;
                case TierEnum.Rare:    multiplier = 2; break;
                case TierEnum.Special: multiplier = 3; break;
            }

            return baseValue * multiplier;
        }

        private static int GetIntFromCardValue(CardValue value)
        {
            switch (value)
            {
                case CardValue.V0: return 0;
                case CardValue.V1: return 1;
                case CardValue.V2: return 2;
                case CardValue.V3: return 3;
                case CardValue.V4: return 4;
                case CardValue.V5: return 5;
                case CardValue.V6: return 6;
                default: return -1;
            }
        }
    }
}