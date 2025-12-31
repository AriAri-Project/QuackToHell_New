using UnityEngine;

namespace Court
{
    public static class CourtGameRules
    {
        /// <summary>
        /// 두 카드가 증거물로 제출 가능한 조합인지 검사
        /// </summary>
        public static bool IsCompatible(global::CardItemData cardA, global::CardItemData cardB)
        {
            // 1. 카드 원본 ID(Key) 가져오기
            int idA = cardA.cardIdKey;
            int idB = cardB.cardIdKey;

            // 2. DeckManager(Global Namespace)를 통해 카드 정의(Def) 조회
            if (!global::DeckManager.Instance.TryGetCardDefinition(idA, out global::CardDef defA) ||
                !global::DeckManager.Instance.TryGetCardDefinition(idB, out global::CardDef defB))
            {
                Debug.LogWarning($"[CourtGameRules] 카드 정의를 찾을 수 없습니다. Key: {idA}, {idB}");
                return false; 
            }

            // 3. 타입 검사: 하나는 숫자(Number), 하나는 기호(Operator)여야 함
            bool hasNumber = defA.type == global::TypeEnum.Number || defB.type == global::TypeEnum.Number;
            bool hasOperator = defA.type == global::TypeEnum.Operator || defB.type == global::TypeEnum.Operator;

            if (!hasNumber || !hasOperator) return false; 

            // 4. 역할 분담 (누가 숫자고 누가 기호인지 구분)
            global::CardDef numberDef = (defA.type == global::TypeEnum.Number) ? defA : defB;
            global::CardDef operatorDef = (defA.type == global::TypeEnum.Operator) ? defA : defB;

            // 5. 특수 카드 처리 ('N' 카드 등)
            if (numberDef.subType == global::SubTypeEnum.N || numberDef.Value == global::CardValue.N) 
            {
                return true; 
            }

            // 6. 기호 카드의 등급(Tier)에 따른 숫자 호환성 체크
            int numVal = GetIntFromCardValue(numberDef.Value);
            if (numVal == -1) return false; 

            switch (operatorDef.tier)
            {
                case global::TierEnum.Common: // 동(Bronze): 1 ~ 2
                    return numVal >= 1 && numVal <= 2;

                case global::TierEnum.Rare:   // 은(Silver): 1 ~ 4
                    return numVal >= 1 && numVal <= 4;

                case global::TierEnum.Special:// 금(Gold): 0 ~ 6 (모두)
                    return numVal >= 0 && numVal <= 6;

                default:
                    return false;
            }
        }

        private static int GetIntFromCardValue(global::CardValue value)
        {
            switch (value)
            {
                case global::CardValue.V0: return 0;
                case global::CardValue.V1: return 1;
                case global::CardValue.V2: return 2;
                case global::CardValue.V3: return 3;
                case global::CardValue.V4: return 4;
                case global::CardValue.V5: return 5;
                case global::CardValue.V6: return 6;
                default: return -1;
            }
        }
    }
}