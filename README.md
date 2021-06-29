# MyLab.PrometheusAgent
[![Docker image](https://img.shields.io/docker/v/mylabtools/prometheus-agent?label=Docker%20image)](https://hub.docker.com/r/mylabtools/prometheus-agent)

Ознакомьтесь с последними изменениями в [журнале изменений](/changelog.md).

## Обзор 

Собирает, аггрегирует и предоставляет метрики других сервисов в формате [Prometheus](https://prometheus.io/docs/concepts/data_model/). 

Метрики собираются в момент запроса параллельно со всех целевых сервисов.

## Производительность

`MyLab.PrometheusAgent` является частичной альтернативой сервиса [Prometheus](https://prometheus.io/), развёрнутого с целью использования в [федеративном режиме](https://prometheus.io/docs/prometheus/latest/federation/). Поэтому в данном разделе сравнение происходит с ним.

Тестовое окружение:

* RAM: **8 Gb**
* CPU: **2х Intel(R) Xeon(R) CPU E5-2630 v4 @ 2.20GHz**
* Целевых сервисов: **75**

| Решение               | Потребление памяти | Потребление процессора |
| --------------------- | ------------------ | ---------------------- |
| MyLab.PrometheusAgent | до 290 Mb          | ~0.1%                  |
| Prometheus            | ~1.16 Gb           | ~20%                   |

## API

### GET /metrics

Возвращает собранные метрики

Пример:

```http
200 OK

Content-Type: text/plain; charset=utf-8

# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="1",instance="localhost:10201",job="job1"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="1",instance="localhost:10200",job="job1"} 1.10
# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="2",instance="localhost:10201",job="job1"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="2",instance="localhost:10200",job="job1"} 1.10
# TYPE bar_metric counter
bar_metric {label3="value3",label4="value4",target_batch="1",instance="localhost:10201",job="job2"} 2.20 1624868358000
# TYPE foo_metric gauge
foo_metric {label1="value1",label2="value2",target_batch="1",instance="localhost:10200",job="job2"} 1.10
```

### GET /config

Возвращает конфигурацию сбора метрик в `JSON` представлении.

Пример:

```http
200 OK

Content-Type: application/json; charset=utf-8
Content-Length: 290

{
  "Items": [
    {
      "JobName": "job1",
      "StaticConfigs": [
        {
          "Targets": [
            "localhost:10200",
            "localhost:10201"
          ],
          "Labels": {
            "target_batch": "1"
          }
        }
      ]
    }
  ]
}
```

## Конфигурация

### ScrapeConfig

Конфигурация сборки метрик. Путь к файлу с настройками задаётся переменной окружения `PROMETHEUS_AGENT__CONFIG`.

Формат файла соответствует урезанной спецификации конфигурационного файла Prometheus в части [scrape_config](https://prometheus.io/docs/prometheus/latest/configuration/configuration/#scrape_config).

Поддерживаемые узлы:

| Путь                                      | Описание                                                     |
| ----------------------------------------- | ------------------------------------------------------------ |
| scrape_configs                            | Секции, описывающие работы по сборке метрик: подключения и параметры |
| scrape_configs[]/job_name                 | Наименование работы, ассоциированное со сборкой метрик       |
| scrape_configs[]/static_configs           | Указывает список подключений и меток                         |
| scrape_configs[]/static_configs[]/targets | Целевые подключения (хост+порт)                              |
| scrape_configs[]/static_configs[]/labels  | Метки, которые будут добавляться ко всем метрикам, полученным из указанных в работе целевых подключений |

Пример конфигурационного файла:

```yaml
scrape_configs:

- job_name: job1
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 2
      
- job_name: job2
  static_configs:
  - targets:
    - localhost:10200
    - localhost:10201
    labels:
      target_batch: 1

```

### Фиксированные настройки

* таймаут сбора всех метрик - 20 сек
* таймаут сбора метрик с одного подключения - 15 сек
* путь сборки метрик - `/metrics`

## Развёртывание

Сервис адаптирован для развёртывания на `docker`. При этом потребуется:

* подклчить конфиг сбора метрик;
* указать путь к нему через переменную окружения `PROMETHEUS_AGENT__CONFIG`.

Пример docker-compose файла:

```yaml
version: '3.2'

services:
  prometheus-agent:
    image: mylabtools/prometheus-agent
	container_name: prometheus-agent
    volumes:
    - ./scrape_config.yml:/scrape_config.yml
    environment:
    - PROMETHEUS_AGENT__CONFIG=/scrape_config.yml
    
```

