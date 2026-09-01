using UnityEngine;

/// <summary>
/// 일러스트 슬롯(CharacterIllustrationView)이 표시할 수 있는 원본. "무엇을 어떻게 프레이밍할지" 를
/// 아트가 직접 선언하게 해서, 슬롯이 데이터 종류(캐릭터/장비/...)를 알 필요가 없게 한다.
///
/// 슬롯은 이 인터페이스만 알고, 새 데이터 종류가 생겨도 슬롯 코드는 바뀌지 않는다.
/// 반대로 같은 슬롯에 캐릭터와 장비를 섞어 넣어도 각자 올바른 방식으로 잡힌다(가챠 확률표/로테이션 목록 등).
/// </summary>
public interface IIllustrationSource {
    /// <summary>표시할 원본 스프라이트. 없으면 null(슬롯이 비워진다).</summary>
    Sprite Illustration { get; }

    /// <summary>일러스트 내 관심점(정규화 0~1). Crop 프레이밍에서 이 점이 슬롯 기준점에 놓인다.</summary>
    Vector2 UiFocus { get; }

    /// <summary>이 아트가 요구하는 프레이밍 방식. 잘려도 되는 그림인지, 전체가 보여야 하는 그림인지를 아트가 결정한다.</summary>
    IllustrationFit Fit { get; }
}

/// <summary>
/// 일러스트를 슬롯(마스크) 안에 넣는 방식.
///
/// 둘 다 원본 픽셀수/화면 해상도와 무관한 상대 계산이므로, 슬롯 크기가 달라져도 같은 결과가 나온다.
/// </summary>
public enum IllustrationFit {
    /// <summary>
    /// 슬롯 높이를 채우고 넘치는 부분은 마스크가 잘라낸다(크롭). 관심점 프레이밍이 의미를 갖는 방식으로,
    /// 전신 일러스트에서 얼굴만 뽑아 쓰는 캐릭터 초상이 여기에 해당한다.
    /// </summary>
    Crop,

    /// <summary>
    /// 그림 전체가 슬롯 안에 들어오도록 맞춘다(크롭 없음). 잘리면 무엇인지 알 수 없는 그림,
    /// 즉 투명 여백이 재단되어 가로세로 비율이 제각각인 아이콘류가 여기에 해당한다.
    /// 전체를 보여주는 것이 목적이므로 관심점은 쓰지 않고 그림 중심을 기준점에 놓는다.
    /// </summary>
    Contain
}
