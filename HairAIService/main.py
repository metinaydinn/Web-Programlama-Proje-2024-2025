from flask import Flask, request, jsonify
import cv2
import mediapipe as mp
import numpy as np
import os
import random
# Flask uygulaması
app = Flask(__name__)

# Mediapipe yüz mesh modeli
mp_face_mesh = mp.solutions.face_mesh
face_mesh = mp_face_mesh.FaceMesh(static_image_mode=True, max_num_faces=1)

# Mediapipe yüz tespiti modeli
mp_face_detection = mp.solutions.face_detection

# Yüz şekline göre önerilen saç modelleri ve renkleri
HAIR_RECOMMENDATIONS = {
    "Oval": [
        {"model": "styles/classic_black_hair.png", "color": "#000000"},
        {"model": "styles/classic2_black_wave_hair.png", "color": "#3A3A3A"},
        {"model": "styles/galactic_black_hair.png", "color": "#2F2F4F"},
    ],
    "Yuvarlak": [
        {"model": "anime_shiny_black_hair.png", "color": "#2F2F4F"},
        {"model": "styles/classic_black_hair.png", "color": "#000000"},
        
        {"model": "styles/classic_brown_hair.png", "color": "#8B4513"},
        {"model": "styles/classic2_black_wave_hair.png", "color": "#3A3A3A"},
        {"model": "styles/galactic_black_hair.png", "color": "#2F2F4F"},
        {"model": "styles/light_brown_spiky_hair.png", "color": "#D2B48C"},
        {"model": "styles/messy_brown_hair.png", "color": "#8B4513"},
     
    ],
    "Kare": [
           {"model": "styles/modern_black_side_part.png", "color": "#000000"},
        {"model": "styles/modern_layered_black_hair.png", "color": "#2B2B2B"},
        {"model": "styles/platinum_spiky_hair.png", "color": "#F8F8FF"},
        {"model": "styles/slick_black_hair.png", "color": "#1C1C1C"},
        {"model": "styles/slick_black_side_part_hair.png", "color": "#2B2B2B"},
        {"model": "styles/slick_brown_hair.png", "color": "#4B3621"},
        {"model": "styles/classic_black_wave_hair.png", "color": "#2C2C2C"},
        {"model": "styles/wavy_dark_hair.png", "color": "#1C1C1C"},
    ],
    "Uzun": [
        {"model": "styles/textured_fringe_black.png", "color": "#000000"},
        {"model": "styles/messy_brown_hair.png", "color": "#8B4513"},
        {"model": "styles/textured_fringe_black.png", "color": "#000000"},
    ],
}

# Yüz analizi fonksiyonu
def analyze_face(image_path):
    image = cv2.imread(image_path)
    ih, iw, _ = image.shape  # Görselin yüksekliği ve genişliği
    rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    results = face_mesh.process(rgb_image)

    if not results.multi_face_landmarks:
        return "Yüz tespit edilemedi", None

    face_landmarks = results.multi_face_landmarks[0]
    jaw_points = [face_landmarks.landmark[i] for i in range(0, 17)]  # Çene noktaları

    # Normalleştirilmiş koordinatları piksele dönüştür
    jaw_x = [lm.x * iw for lm in jaw_points]
    jaw_y = [lm.y * ih for lm in jaw_points]

    # Yüz genişliği ve yüksekliğini hesapla
    face_width = abs(jaw_x[-1] - jaw_x[0])  # Sol ve sağ çene arası genişlik
    face_height = max(jaw_y) - min(jaw_y)   # Çene altı ve yüz üstü yüksekliği
    aspect_ratio = face_width / face_height*100  # Genişlik/Yükseklik oranı

    print(f"Face Width (pixels): {face_width}, Face Height (pixels): {face_height}, Aspect Ratio: {aspect_ratio}")

    # Daha fazla yüz şekli sınıflandırması
    if aspect_ratio >= 1.5:
        return "Yuvarlak", image
    elif 1.3 <= aspect_ratio < 1.5:
        return "Oval", image
    elif 1.1 <= aspect_ratio < 1.3:
        return "Uzun", image
    else:
        return "Kare", image  # Eğer oran çok düşükse "Kare" yüz şekli



# Saç modelini yüz üzerine bindirme fonksiyonu
def overlay_hair(image_path, hair_model_path):
    # Görselleri yükle
    image = cv2.imread(image_path, cv2.IMREAD_UNCHANGED)
    hair = cv2.imread(hair_model_path, cv2.IMREAD_UNCHANGED)

    # Mediapipe yüz tespiti
    with mp_face_detection.FaceDetection(min_detection_confidence=0.5) as face_detection:
        image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        results = face_detection.process(image_rgb)

        if not results.detections:
            print("Yüz tespit edilemedi!")
            return None

        # Maske oluştur
        ih, iw, _ = image.shape
        mask = np.ones((ih, iw), dtype=np.uint8) * 255

        for detection in results.detections:
            bboxC = detection.location_data.relative_bounding_box
            x, y, w, h = int(bboxC.xmin * iw), int(bboxC.ymin * ih), int(bboxC.width * iw), int(bboxC.height * ih)

            # Serbest çizgili maske için yüz üstü noktalarını belirle
            points = np.array([
                [x - 40, y - 30],  # Sol üst
                [x + w + 40, y - 30],  # Sağ üst
                [x + w + 50, y + h // 3],  # Sağ orta
                [x + w+150 , y + h+200],  # Sağ alt
                [x -150, y + h+200],  # Sol alt
                [x - 70, y + h // 3]  # Sol orta
            ], np.int32)

            # Serbest çizgili maskeyi oluştur
            cv2.fillPoly(mask, [points], 0)  # Yüzün dışını şeffaf yap

        # Görseli şeffaf yap
        if image.shape[2] == 3:
            image = cv2.cvtColor(image, cv2.COLOR_BGR2BGRA)
        image[:, :, 3][mask == 255] = 0

        # Saç modelini yeniden boyutlandır ve yerleştir
        hair_width = int(w * 1.62)
        hair_height = int(h * 1)
        resized_hair = cv2.resize(hair, (hair_width, hair_height))

        hair_x = x - int((hair_width - w+10) / 1.9)
        hair_y = y - int(hair_height * 0.5)
        image = overlay_image_alpha(image, resized_hair, (hair_x, hair_y))

    return image


def overlay_image_alpha(img, overlay, position):
    """ Alfa kanalına göre bir resmi diğerinin üzerine bindirir. """
    x, y = position
    h, w = overlay.shape[:2]

    for i in range(h):
        for j in range(w):
            if y + i >= img.shape[0] or x + j >= img.shape[1] or y + i < 0 or x + j < 0:
                continue
            alpha = overlay[i, j, 3] / 255.0
            if alpha > 0:
                img[y + i, x + j, :3] = (
                    alpha * overlay[i, j, :3] + (1 - alpha) * img[y + i, x + j, :3]
                )
                img[y + i, x + j, 3] = 255

    return img
from flask import send_from_directory

@app.route('/uploads/<path:filename>')
def serve_uploaded_file(filename):
    return send_from_directory(os.getcwd(), filename)

# Ana API endpoint'i
@app.route('/analyze', methods=['POST'])
def analyze_photo():
    if 'file' not in request.files:
        return jsonify({"error": "Dosya yüklenmedi"}), 400

    file = request.files['file']
    image_path = "uploaded_image.jpg"
    output_path = "output_with_hair.png"
    file.save(image_path)

    # Yüz analizi yap
    face_shape, _ = analyze_face(image_path)
    if not face_shape:
        return jsonify({"error": "Yüz tespit edilemedi"}), 400

    # Rastgele saç modeli seçimi
    recommendations = HAIR_RECOMMENDATIONS.get(face_shape, [{"model": "styles/default.png", "color": "#FFFFFF"}])
    recommendation = random.choice(recommendations)  # Rastgele saç modeli seç

    hair_model_path = recommendation["model"]

    # Saç modelini yüz üzerine bindir
    result_image = overlay_hair(image_path, hair_model_path)
    if result_image is None:
        return jsonify({"error": "Saç modeli bindirme başarısız"}), 500

    # Sonuç görselini kaydet
    cv2.imwrite(output_path, result_image)

    return jsonify({
        "face_shape": face_shape,
        "hair_model": recommendation["model"],
        "hair_color": recommendation["color"],
        "output_image": "http://127.0.0.1:5000/uploads/output_with_hair.png"
    })


if __name__ == '__main__':
    app.run(debug=True)
